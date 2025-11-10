using UnityEngine;
using System;
using FaceLib;

public class GameStateManager : FaceLib.SingletonWithMono<GameStateManager>, IBaseManager
{
    // IBaseManager 구현
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        if (IsInitialized) return;

        ScreenStateListener.OnStateChanged += HandleStateChanged;
        IsInitialized = true;
        Debug.Log("GameStateManager initialized");
    }

    public void Shutdown()
    {
        if (!IsInitialized) return;

        ScreenStateListener.OnStateChanged -= HandleStateChanged;
        IsInitialized = false;
        Debug.Log("GameStateManager shutdown");
    }

    // 기존 Init 메서드는 Initialize로 리다이렉트 (하위 호환성)
    public void Init()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        SaveCurrentStateTime();
        Shutdown();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            // 백그라운드로 갈 때 현재 상태 시간 저장
            // 상태 변경 이벤트가 발생하기 전에 현재 상태의 시간을 저장
            Debug.Log("[GameStateManager] App paused, saving current state time before state change");
            SaveCurrentStateTime();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // 포커스를 잃을 때 현재 상태 시간 저장
            // 상태 변경 이벤트가 발생하기 전에 현재 상태의 시간을 저장
            Debug.Log("[GameStateManager] App lost focus, saving current state time before state change");
            SaveCurrentStateTime();
        }
        else
        {
            // 포그라운드로 돌아올 때도 현재 상태 시간 저장 (복귀 전 상태)
            Debug.Log("[GameStateManager] App gained focus, saving current state time");
            SaveCurrentStateTime();
        }
    }

    private void OnApplicationQuit()
    {
        // 앱 종료 시 현재 상태 시간 저장
        SaveCurrentStateTime();
    }

    /// <summary>
    /// 현재 상태의 경과 시간을 저장합니다.
    /// </summary>
    private void SaveCurrentStateTime()
    {
        if (!IsInitialized || ScreenStateListener.Instance == null)
            return;

        var currentState = ScreenStateListener.Instance.Current;
        if (currentState == ScreenStateListener.DeviceScreenState.Unknown)
            return;

        var elapsed = ScreenStateListener.Instance.GetCurrentStateElapsed();
        if (elapsed.TotalSeconds <= 0)
            return;

        Debug.Log($"[GameStateManager] Saving current state time - State: {currentState}, Duration: {elapsed.TotalSeconds:F1}s");

        // GameData에 시간 누적
        GameData gameData = Data.Instance.gameData;
        AddTimeToGameData(gameData, currentState, (float)elapsed.TotalSeconds);
        
        // 저장
        Data.Instance.SaveGameData();
        
        // 저장한 시간만큼 StateStartTime 리셋 (중복 저장 방지)
        ScreenStateListener.Instance.ResetStateStartTime();
        
        Debug.Log($"[GameStateManager] Saved current state time - {currentState}: {elapsed.TotalSeconds:F1}s");
    }

    void HandleStateChanged(ScreenStateListener.DeviceScreenState prevState, TimeSpan duration)
    {
        Debug.Log($"[GameStateManager] 이전 상태 {prevState}가 {duration.TotalSeconds:F1}초 동안 유지됨");

        // 이전 상태의 시간을 누적 (Unknown이 아닌 경우만)
        if (prevState != ScreenStateListener.DeviceScreenState.Unknown && duration.TotalSeconds > 0)
        {
            // GameData에 시간 누적
            GameData gameData = Data.Instance.gameData;
            
            // 저장 전 현재 Total 값 확인
            float beforeTotal = GetTotalTime(gameData, prevState);
            
            // 이전 상태의 시간을 누적
            AddTimeToGameData(gameData, prevState, (float)duration.TotalSeconds);
            
            // 저장 후 Total 값 확인
            float afterTotal = GetTotalTime(gameData, prevState);
            
            Debug.Log($"[GameStateManager] Before: {beforeTotal:F1}s, Adding: {duration.TotalSeconds:F1}s, After: {afterTotal:F1}s");
            
            // 즉시 저장
            Data.Instance.SaveGameData();
            Debug.Log($"[GameStateManager] Immediately saved after state change - {prevState}: {duration.TotalSeconds:F1}s (Total: {beforeTotal:F1}s -> {afterTotal:F1}s)");
        }
        else
        {
            Debug.LogWarning($"[GameStateManager] Skipped saving - prevState: {prevState}, duration: {duration.TotalSeconds:F1}s");
        }

        // 현재 상태를 ScreenStateListener에서 바로 가져오기
        var current = ScreenStateListener.Instance.Current;

        switch (current)
        {
            case ScreenStateListener.DeviceScreenState.ScreenOff:
                Debug.Log("화면 꺼짐 상태");
                break;

            case ScreenStateListener.DeviceScreenState.Unlocked:
                Debug.Log("잠금 해제됨 상태");
                break;

            case ScreenStateListener.DeviceScreenState.AppRunning:
                Debug.Log("앱 실행중 상태");
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// GameData에 특정 상태의 시간을 누적합니다.
    /// </summary>
    private void AddTimeToGameData(GameData gameData, ScreenStateListener.DeviceScreenState state, float duration)
    {
        if (gameData == null)
        {
            Debug.LogError("[GameStateManager] AddTimeToGameData: gameData is null");
            return;
        }

        float beforeValue = 0f;
        switch (state)
        {
            case ScreenStateListener.DeviceScreenState.AppRunning:
                beforeValue = gameData.totalRunningTime;
                gameData.totalRunningTime += duration;
                Debug.Log($"[GameStateManager] AddTime AppRunning: {beforeValue:F1}s + {duration:F1}s = {gameData.totalRunningTime:F1}s");
                break;
            case ScreenStateListener.DeviceScreenState.ScreenOff:
                beforeValue = gameData.totalScreenOffTime;
                gameData.totalScreenOffTime += duration;
                Debug.Log($"[GameStateManager] AddTime ScreenOff: {beforeValue:F1}s + {duration:F1}s = {gameData.totalScreenOffTime:F1}s");
                break;
            case ScreenStateListener.DeviceScreenState.Unlocked:
                beforeValue = gameData.totalUnlockedTime;
                gameData.totalUnlockedTime += duration;
                Debug.Log($"[GameStateManager] AddTime Unlocked: {beforeValue:F1}s + {duration:F1}s = {gameData.totalUnlockedTime:F1}s");
                break;
        }
    }

    /// <summary>
    /// 특정 상태의 Total 시간을 가져옵니다.
    /// </summary>
    private float GetTotalTime(GameData gameData, ScreenStateListener.DeviceScreenState state)
    {
        if (gameData == null) return 0f;

        switch (state)
        {
            case ScreenStateListener.DeviceScreenState.AppRunning:
                return gameData.totalRunningTime;
            case ScreenStateListener.DeviceScreenState.ScreenOff:
                return gameData.totalScreenOffTime;
            case ScreenStateListener.DeviceScreenState.Unlocked:
                return gameData.totalUnlockedTime;
            default:
                return 0f;
        }
    }
}
