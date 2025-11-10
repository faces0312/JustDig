using UnityEngine;
using FaceLib;

public class GameManager : BaseGameManager
{
    protected override void AddPreLoadManagers()
    {
        // 저장된 데이터가 먼저 로드되도록 보장
        Data.Instance.EnsureGameDataLoaded();

        // PreLoad 단계: 다른 Manager들보다 먼저 초기화되어야 하는 Manager들
        // ScreenStateListener는 이벤트 발행자이므로 먼저 초기화
        _preLoadManagerList.Add(ScreenStateListener.Instance);
    }

    protected override void AddManagers()
    {
        // 일반 Manager들: PreLoad 이후 초기화
        // GameStateManager는 ScreenStateListener의 이벤트를 구독하므로 나중에 초기화
        _managerList.Add(GameStateManager.Instance);
        _managerList.Add(StageManager.Instance);
    }

    protected override void OnPreLoad()
    {
        base.OnPreLoad();
        Debug.Log("PreLoad Managers initialized");
    }

    protected override void OnInit()
    {
        base.OnInit();
        Debug.Log("All Managers initialized in correct order");
    }
}
