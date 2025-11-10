using UnityEngine;
using FaceLib;
using UnityEngine.UI;
using TMPro;
using System;

public class UIGrpGameState : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI tmpScreenOFF;
    public TextMeshProUGUI tmpScreenON;
    public TextMeshProUGUI tmpScreenInGame;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.1f; // UI 업데이트 간격 (초)

    private float updateTimer = 0f;
    private bool isApplicationPaused = false;

    private void OnEnable()
    {
        ScreenStateListener.OnStateChanged += HandleStateChanged;
        // 초기화 시 저장된 데이터 표시를 위해 약간의 지연 후 UI 업데이트
        StartCoroutine(DelayedInitialUpdate());
    }

    private System.Collections.IEnumerator DelayedInitialUpdate()
    {
        // Data가 로드될 때까지 대기
        while (Data.Instance == null)
        {
            yield return null;
        }

        while (Data.Instance.gameData == null)
        {
            Data.Instance.EnsureGameDataLoaded();
            yield return null;
        }

        // 첫 프레임 대기 (ScreenStateListener 초기화 대기)
        yield return null;
        // 저장된 데이터를 즉시 표시
        UpdateUI();
    }

    private void OnDisable()
    {
        ScreenStateListener.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        // 포그라운드에 있을 때만 업데이트
        if (isApplicationPaused) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateUI();
        }
    }

    // 앱이 포그라운드로 복귀할 때 UI 업데이트
    private void OnApplicationPause(bool paused)
    {
        Debug.Log($"[UIGrpGameState] OnApplicationPause: {paused}");
        isApplicationPaused = paused;
        if (!paused) // 포그라운드 복귀
        {
            Debug.Log("[UIGrpGameState] App resumed, updating UI");
            UpdateUI(); // 즉시 UI 업데이트
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"[UIGrpGameState] OnApplicationFocus: {hasFocus}");
        if (hasFocus) // 포커스 획득
        {
            isApplicationPaused = false;
            Debug.Log("[UIGrpGameState] App focused, updating UI");
            UpdateUI(); // 즉시 UI 업데이트
        }
        else
        {
            isApplicationPaused = true;
        }
    }

    private void HandleStateChanged(ScreenStateListener.DeviceScreenState prevState, TimeSpan duration)
    {
        // 상태 변경 시 즉시 UI 업데이트 (저장된 total 표시)
        var newState = ScreenStateListener.Instance?.Current;
        Debug.Log($"[UIGrpGameState] State changed: {prevState} -> {newState}, Duration: {duration.TotalSeconds:F1}s, isApplicationPaused: {isApplicationPaused}");
        
        // 항상 UI 업데이트 (백그라운드에서도 상태는 저장되어 포그라운드 복귀 시 표시됨)
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Data 싱글톤 인스턴스를 통해 GameData 사용
        Data data = Data.Instance;
        if (data == null)
        {
            Debug.LogError("[UIGrpGameState] Data.Instance is null!");
            return;
        }

        GameData gameData = data.gameData;
        if (gameData == null)
        {
            Debug.LogError("[UIGrpGameState] Data.Instance.gameData is null!");
            return;
        }
        
        Debug.Log($"[UIGrpGameState] Using GameData - Running: {gameData.totalRunningTime:F1}s, ScreenOff: {gameData.totalScreenOffTime:F1}s, Unlocked: {gameData.totalUnlockedTime:F1}s");
        
        // 값이 모두 0이면 경고
        if (gameData.totalRunningTime == 0 && gameData.totalScreenOffTime == 0 && gameData.totalUnlockedTime == 0)
        {
            Debug.LogWarning("[UIGrpGameState] All GameData values are 0 - might be new data or load issue");
        }

        // ScreenStateListener가 초기화되지 않았거나 현재 상태를 알 수 없는 경우
        if (ScreenStateListener.Instance == null)
        {
            Debug.LogWarning("[UIGrpGameState] ScreenStateListener.Instance is null, showing saved totals only");
            // 저장된 Total 값만 표시 (현재 상태는 표시하지 않음)
            ShowSavedTotalsOnly(gameData);
            return;
        }

        var currentState = ScreenStateListener.Instance.Current;
        var elapsed = ScreenStateListener.Instance.GetCurrentStateElapsed();

        Debug.Log($"[UIGrpGameState] UpdateUI - Current State: {currentState}, Elapsed: {elapsed.TotalSeconds:F1}s, isApplicationPaused: {isApplicationPaused}");

        // 현재 상태에 따라 해당 UI만 표시
        switch (currentState)
        {
            case ScreenStateListener.DeviceScreenState.ScreenOff:
                if (tmpScreenOFF != null)
                {
                    tmpScreenOFF.text = $"ScreenOff\nCurrent: {elapsed.TotalSeconds:F1}s\nTotal: {gameData.totalScreenOffTime:F1}s";
                    Debug.Log($"[UIGrpGameState] Showing ScreenOff UI - Text: {tmpScreenOFF.text}");
                }
                else
                {
                    Debug.LogWarning("[UIGrpGameState] tmpScreenOFF is null!");
                }
                break;

            case ScreenStateListener.DeviceScreenState.Unlocked:
                if (tmpScreenON != null)
                {
                    tmpScreenON.text = $"Unlocked\nCurrent: {elapsed.TotalSeconds:F1}s\nTotal: {gameData.totalUnlockedTime:F1}s";
                    Debug.Log($"[UIGrpGameState] Showing Unlocked UI - Text: {tmpScreenON.text}");
                }
                else
                {
                    Debug.LogWarning("[UIGrpGameState] tmpScreenON is null!");
                }
                break;

            case ScreenStateListener.DeviceScreenState.AppRunning:
                if (tmpScreenInGame != null)
                {
                    tmpScreenInGame.text = $"AppRunning\nCurrent: {elapsed.TotalSeconds:F1}s\nTotal: {gameData.totalRunningTime:F1}s";
                    Debug.Log($"[UIGrpGameState] Showing AppRunning UI - Text: {tmpScreenInGame.text}");
                }
                else
                {
                    Debug.LogWarning("[UIGrpGameState] tmpScreenInGame is null!");
                }
                break;

            case ScreenStateListener.DeviceScreenState.Unknown:
                // Unknown 상태일 때도 저장된 Total 값 표시
                Debug.LogWarning("[UIGrpGameState] Current state is Unknown, showing saved totals only");
                ShowSavedTotalsOnly(gameData);
                break;

            default:
                Debug.LogWarning($"[UIGrpGameState] Unknown state: {currentState}");
                ShowSavedTotalsOnly(gameData);
                break;
        }
    }

    /// <summary>
    /// 저장된 Total 값만 표시합니다 (현재 상태를 알 수 없을 때 사용)
    /// </summary>
    private void ShowSavedTotalsOnly(GameData gameData)
    {
        // 모든 UI에 저장된 Total 값 표시 (Current는 0으로 표시)
        if (tmpScreenOFF != null)
        {
            tmpScreenOFF.text = $"ScreenOff\nCurrent: 0.0s\nTotal: {gameData.totalScreenOffTime:F1}s";
        }
        if (tmpScreenON != null)
        {
            tmpScreenON.text = $"Unlocked\nCurrent: 0.0s\nTotal: {gameData.totalUnlockedTime:F1}s";
        }
        if (tmpScreenInGame != null)
        {
            tmpScreenInGame.text = $"AppRunning\nCurrent: 0.0s\nTotal: {gameData.totalRunningTime:F1}s";
        }
        Debug.Log($"[UIGrpGameState] Showing saved totals only - Running: {gameData.totalRunningTime:F1}s, ScreenOff: {gameData.totalScreenOffTime:F1}s, Unlocked: {gameData.totalUnlockedTime:F1}s");
    }

}
