using System;
using UnityEngine;

public class ScreenStateListener : Singleton<ScreenStateListener>
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

    public void Init()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        _activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        _bridge = new AndroidJavaClass("com.screenstate.ScreenStateBridge");
        _bridge.CallStatic("register", _activity);
#endif

        // 앱 시작을 AppRunning으로 간주
        SetState(DeviceScreenState.AppRunning);
    }

    private void OnApplicationQuit()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_bridge != null && _activity != null)
            _bridge.CallStatic("unregister", _activity);
#endif
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_bridge != null && _activity != null)
            _bridge.CallStatic("unregister", _activity);
#endif
    }

    // ====== Java -> Unity 콜백 ======
    public void OnAndroidScreenState(string state)
    {
        if (state == "SCREEN_OFF")
            SetState(DeviceScreenState.ScreenOff);
        else if (state == "USER_PRESENT")
            SetState(DeviceScreenState.Unlocked);
        // SCREEN_ON은 요구사항 밖이면 무시
    }

    // ====== Unity 생명주기 ======
    private void OnApplicationPause(bool paused)
    {
        if (!paused) // 포그라운드 복귀
            SetState(DeviceScreenState.AppRunning);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused) // 포커스 획득
            SetState(DeviceScreenState.AppRunning);
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

    /// <summary>씬 어디에도 없으면 자동 생성하고 반환 (선택적 사용).</summary>
    public static ScreenStateListener EnsureInstance()
    {
        return Instance; // Singleton<T>에서 자동으로 처리됨
    }
}