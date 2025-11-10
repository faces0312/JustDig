using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class Data : MonoBehaviour
{
    static GameObject _container;

    static GameObject Container
    {
        get
        {
            return _container;
        }
    }
    static Data _instance;

    public static Data Instance
    {
        get
        {
            if (!_instance)
            {
                _container = new GameObject();
                _container.name = "Data";
                _instance = _container.AddComponent(typeof(Data)) as Data;
                DontDestroyOnLoad(_container);
            }
            return _instance;
        }
    }

    private const string GameDataFileName = "monsterXhunter.json";  //이름 변경 절대 X 



    private GameData _gameData;

    public GameData gameData
    {
        get
        {
            if (_gameData == null)
            {
                EnsureGameDataLoaded();
            }
            return _gameData;
        }
    }

    public void EnsureGameDataLoaded()
    {
        if (_gameData != null) return;
        LoadGameData();
        if (_gameData == null)
            _gameData = new GameData();
    }

    private void Awake()
    {
        EnsureGameDataLoaded();
    }

    public void LoadGameData()
    {
        string filePath = Application.persistentDataPath + "/" + GameDataFileName;
        if (File.Exists(filePath))
        {
            try
            {
                string FromJsonData = File.ReadAllText(filePath);
                _gameData = JsonUtility.FromJson<GameData>(FromJsonData);
                
                // 로드된 데이터가 null이면 새로 생성
                if (_gameData == null)
                {
                    Debug.LogWarning("[Data] 로드된 데이터가 null, 새로 생성");
                    _gameData = new GameData();
                }
                else
                {
                    Debug.Log($"[Data] 불러오기 성공 - Running: {_gameData.totalRunningTime:F1}s, ScreenOff: {_gameData.totalScreenOffTime:F1}s, Unlocked: {_gameData.totalUnlockedTime:F1}s");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Data] 불러오기 실패: {e.Message}");
                // 기존 데이터가 있으면 유지, 없으면 새로 생성
                if (_gameData == null)
                {
                    _gameData = new GameData();
                }
            }
        }
        else
        {
            Debug.Log("[Data] 저장 파일이 없음, 새로 생성");
            _gameData = new GameData();
        }
    }

    public void SaveGameData()
    {
        if (_gameData == null)
        {
            Debug.LogWarning("[Data] SaveGameData: _gameData is null, cannot save");
            return;
        }
        
        string ToJsonData = JsonUtility.ToJson(_gameData);
        string filePath = Application.persistentDataPath + "/" + GameDataFileName;
        
        try
        {
            File.WriteAllText(filePath, ToJsonData);
            Debug.Log($"[Data] 저장 완료 - Running: {_gameData.totalRunningTime:F1}s, ScreenOff: {_gameData.totalScreenOffTime:F1}s, Unlocked: {_gameData.totalUnlockedTime:F1}s");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Data] 저장 실패: {e.Message}");
        }
    }


    private void OnApplicationQuit()
    {
        SaveGameData();
    }
}
