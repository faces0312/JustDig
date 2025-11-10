using UnityEngine;
using System;

public class GameStateManager : Singleton<GameStateManager>
{
    public void Init()
    {
        ScreenStateListener.OnStateChanged += HandleStateChanged;
        Debug.Log("GameStateManager initialized");
    }

    private void OnDestroy()
    {
        ScreenStateListener.OnStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(ScreenStateListener.DeviceScreenState prevState, TimeSpan duration)
    {
        Debug.Log($"이전 상태 {prevState}가 {duration.TotalSeconds:F1}초 동안 유지됨");

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
}
