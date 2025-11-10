using UnityEngine;
using FaceLib;

public class StageManager : FaceLib.SingletonWithMono<StageManager>, IBaseManager
{
    // IBaseManager 구현
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        if (IsInitialized) return;

        // 무한 맵 시스템 초기화 로직 추가 예정
        IsInitialized = true;
        Debug.Log("StageManager initialized");
    }

    public void Shutdown()
    {
        if (!IsInitialized) return;

        // 정리 로직 추가 예정
        IsInitialized = false;
        Debug.Log("StageManager shutdown");
    }

    // 기존 Init 메서드는 Initialize로 리다이렉트 (하위 호환성)
    public void Init()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        Shutdown();
    }
}
