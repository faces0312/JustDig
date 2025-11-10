using System;
using UnityEngine;
using FaceLib;

public class ScreenStateListener : FaceLib.SingletonWithMono<ScreenStateListener>, IBaseManager
{
    public enum DeviceScreenState
    {
        Unknown,
        AppRunning,   // 앱 실행중 (포그라운드)
        ScreenOff,    // 화면 꺼짐
        Unlocked      // 잠금 해제
    }

    public DeviceScreenState Current { get; private set; } = DeviceScreenState.Unknown;

    // 현재 상태가 시작된 시각 (UTC)
    public DateTime StateStartTime { get; private set; }

    // 지난 상태와 상태 전환 시간 알림 이벤트 (prevState, duration)
    public static event Action<DeviceScreenState, TimeSpan> OnStateChanged;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _activity;
    private AndroidJavaClass _bridge;
#endif

    // IBaseManager 구현
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        if (IsInitialized) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        _activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        _bridge = new AndroidJavaClass("com.screenstate.ScreenStateBridge");
        _bridge.CallStatic("register", _activity);
        
        // 초기 화면 상태 확인
        CheckScreenState();
#else
        isScreenOn = true;
#endif

        // 초기 상태: 앱이 포그라운드에 있으므로 AppRunning
        isAppInForeground = true;
        UpdateStateBasedOnConditions();
        
        IsInitialized = true;
        Debug.Log("ScreenStateListener initialized");
    }

    public void Shutdown()
    {
        if (!IsInitialized) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (_bridge != null && _activity != null)
            _bridge.CallStatic("unregister", _activity);
        _bridge = null;
        _activity = null;
#endif

        IsInitialized = false;
        Debug.Log("ScreenStateListener shutdown");
    }

    // 기존 Init 메서드는 Initialize로 리다이렉트 (하위 호환성)
    public void Init()
    {
        Initialize();
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    // 앱 실행 상태 추적
    private bool isAppInForeground = true;
    private bool isScreenOn = true;

    // ====== Java -> Unity 콜백 ======
    public void OnAndroidScreenState(string state)
    {
        Debug.Log($"[ScreenStateListener] Native callback: {state}");
        
        if (state == "SCREEN_OFF")
        {
            isScreenOn = false;
            UpdateStateBasedOnConditions();
        }
        else if (state == "SCREEN_ON")
        {
            isScreenOn = true;
            UpdateStateBasedOnConditions();
        }
        else if (state == "USER_PRESENT")
        {
            // 잠금 해제됨
            isScreenOn = true;
            UpdateStateBasedOnConditions();
        }
    }

    // ====== Unity 생명주기 ======
    private void OnApplicationPause(bool paused)
    {
        Debug.Log($"[ScreenStateListener] OnApplicationPause: {paused}");
        isAppInForeground = !paused;
        
        // 백그라운드로 갈 때 화면 상태 확인 시도
        if (paused)
        {
            CheckScreenState();
        }
        
        UpdateStateBasedOnConditions();
    }

    private void OnApplicationFocus(bool focused)
    {
        Debug.Log($"[ScreenStateListener] OnApplicationFocus: {focused}");
        isAppInForeground = focused;
        
        // 포그라운드로 복귀할 때 화면 상태 확인
        if (focused)
        {
            CheckScreenState();
        }
        
        UpdateStateBasedOnConditions();
    }

    // 화면 상태 확인 (네이티브 코드를 통해)
    private void CheckScreenState()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (_activity != null)
            {
                // PowerManager를 통해 화면 상태 확인
                var contextClass = new AndroidJavaClass("android.content.Context");
                var powerService = contextClass.GetStatic<string>("POWER_SERVICE");
                var powerManager = _activity.Call<AndroidJavaObject>("getSystemService", powerService);
                
                if (powerManager != null)
                {
                    bool screenOn = powerManager.Call<bool>("isInteractive");
                    Debug.Log($"[ScreenStateListener] Screen is interactive: {screenOn}");
                    isScreenOn = screenOn;
                }
                else
                {
                    Debug.LogWarning("[ScreenStateListener] PowerManager is null");
                }
            }
            else
            {
                Debug.LogWarning("[ScreenStateListener] Activity is null, cannot check screen state");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ScreenStateListener] Failed to check screen state: {e.Message}");
            // 기본값 유지
        }
#endif
    }

    // ====== 상태 결정 로직 ======
    private void UpdateStateBasedOnConditions()
    {
        DeviceScreenState newState;

        Debug.Log($"[ScreenStateListener] UpdateStateBasedOnConditions - isAppInForeground: {isAppInForeground}, isScreenOn: {isScreenOn}");

        if (isAppInForeground)
        {
            // 게임 실행중 (포그라운드)
            newState = DeviceScreenState.AppRunning;
            Debug.Log("[ScreenStateListener] State determined: AppRunning (foreground)");
        }
        else
        {
            // 게임이 백그라운드에 있음
            if (!isScreenOn)
            {
                // 백그라운드 + 화면 꺼짐
                newState = DeviceScreenState.ScreenOff;
                Debug.Log("[ScreenStateListener] State determined: ScreenOff (background + screen off)");
            }
            else
            {
                // 백그라운드 + 화면 켜짐 (홈버튼 눌러서 정지했거나 다른 앱 사용 중)
                newState = DeviceScreenState.Unlocked;
                Debug.Log("[ScreenStateListener] State determined: Unlocked (background + screen on)");
            }
        }

        SetState(newState);
    }

    // ====== 상태 변경 공통 처리 ======
    private void SetState(DeviceScreenState newState)
    {
        if (Current == newState) return;

        DateTime now = DateTime.UtcNow;
        TimeSpan duration = TimeSpan.Zero;

        if (Current != DeviceScreenState.Unknown)
            duration = now - StateStartTime;

        Debug.Log($"[ScreenState] {Current} -> {newState} (lasted {duration.TotalSeconds:F1}s)");

        // 지난 상태 지속시간 알림
        OnStateChanged?.Invoke(Current, duration);

        // 새 상태 기록
        Current = newState;
        StateStartTime = now;
    }

    // ====== 편의 기능 ======
    /// <summary>현재 상태가 유지된 경과 시간(UTC 기준)을 반환.</summary>
    public TimeSpan GetCurrentStateElapsed()
    {
        if (Current == DeviceScreenState.Unknown) return TimeSpan.Zero;
        return DateTime.UtcNow - StateStartTime;
    }

    /// <summary>
    /// 현재 상태의 시작 시간을 현재 시각으로 리셋합니다.
    /// (저장된 시간만큼 경과 시간을 리셋하기 위해 사용)
    /// </summary>
    public void ResetStateStartTime()
    {
        if (Current == DeviceScreenState.Unknown) return;
        StateStartTime = DateTime.UtcNow;
        Debug.Log($"[ScreenStateListener] Reset StateStartTime for {Current}");
    }

    /// <summary>씬 어디에도 없으면 자동 생성하고 반환 (선택적 사용).</summary>
    public static ScreenStateListener EnsureInstance()
    {
        return Instance; // Singleton<T>에서 자동으로 처리됨
    }

#if UNITY_EDITOR
    // ====== 에디터 전용 테스트 기능 ======
    [ContextMenu("Test: Set App Running")]
    public void TestAppRunning()
    {
        isAppInForeground = true;
        isScreenOn = true;
        UpdateStateBasedOnConditions();
    }

    [ContextMenu("Test: Set Unlocked (Background + Screen On)")]
    public void TestUnlocked()
    {
        isAppInForeground = false;
        isScreenOn = true;
        UpdateStateBasedOnConditions();
    }

    [ContextMenu("Test: Set Screen Off (Background + Screen Off)")]
    public void TestScreenOff()
    {
        isAppInForeground = false;
        isScreenOn = false;
        UpdateStateBasedOnConditions();
    }

    // 키보드 단축키로 테스트
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            TestAppRunning();
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            TestUnlocked();
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            TestScreenOff();
    }
#endif
}