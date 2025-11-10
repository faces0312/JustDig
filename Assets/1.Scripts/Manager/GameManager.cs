using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        // 1. 먼저 ScreenStateListener 초기화 (이벤트 발행자)
        ScreenStateListener.Instance.Init();
        
        // 2. 그 다음 GameStateManager 초기화 (이벤트 구독자)
        GameStateManager.Instance.Init();
        
        // 3. 마지막으로 StageManager 초기화
        StageManager.Instance.Init();
        
        Debug.Log("All singletons initialized in correct order");
    }
}
