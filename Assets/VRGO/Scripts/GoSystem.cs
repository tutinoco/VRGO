
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GoSystem : UdonSharpBehaviour
{
    [Header("管理する碁石を設定します")]
    [SerializeField] private VRCObjectPool blackPool;
    [SerializeField] private VRCObjectPool whitePool;

    void Start()
    {
        if( Networking.IsOwner(gameObject) ) Reset();
    }

    public void Reset()
    {
        if( !Networking.IsOwner(gameObject) ) return; 
        ReturnAll();
        SendCustomEventDelayedSeconds(nameof(SpawnBlack), 0.6f);
        SendCustomEventDelayedSeconds(nameof(SpawnWhite), 0.6f);
    }

    private void ReturnAll()
    {
        for (int i=0; i<blackPool.Pool.Length; i++) {
            GameObject g = blackPool.Pool[i];
            Stone s = (Stone)g.transform.GetComponent(typeof(UdonBehaviour));
            Networking.SetOwner(Networking.LocalPlayer, g);
            s.Return();
        }

        for (int i=0; i<whitePool.Pool.Length; i++) {
            GameObject g = whitePool.Pool[i];
            Stone s = (Stone)g.transform.GetComponent(typeof(UdonBehaviour));
            Networking.SetOwner(Networking.LocalPlayer, g);
            s.Return();
        }
    }

    public Stone SpawnBlack()
    {
        GameObject g = blackPool.TryToSpawn();
        if ( !g ) return null;
        Stone s = (Stone)g.transform.GetComponent(typeof(UdonBehaviour));
        s.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnSpawn)); 
        return s;
    }

    public Stone SpawnWhite()
    {
        GameObject g = whitePool.TryToSpawn();
        if ( !g ) return null;
        Stone s = (Stone)g.transform.GetComponent(typeof(UdonBehaviour));
        s.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnSpawn));
        return s;
    }

    public void killStone( Stone s )
    {
        if ( s.isBlack ) blackPool.Return(s.gameObject);
        if ( !s.isBlack ) whitePool.Return(s.gameObject);
    }
}
