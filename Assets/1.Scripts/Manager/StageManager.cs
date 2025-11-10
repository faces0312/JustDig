using UnityEngine;

public class StageManager : SingletonWithMono<StageManager>
{
    public void Init()
    {
        Debug.Log("StageManager Init");
    }
}
