using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class EnvironmentManager : UdonSharpBehaviour
{
    [Header("管理するGoManagerを設定します")]
    [SerializeField] private GoSystem[] gosyss;
    [SerializeField] private GameObject[] screens;
    [SerializeField] private GameObject[] playAreas;
    [SerializeField] private GameObject[] cameras;

    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(goValue))] private int _goValue;
    public int goValue {
        set {
            _goValue = value;
            int h = gosyss.Length / 2;
            for (int i=0; i<gosyss.Length; i++) {
                bool b = i >= h-_goValue && i <= h+_goValue;
                gosyss[i].gameObject.SetActive(b);
            }
        }
        get { return _goValue; }
    }

    public void AddGo()
    {
        if ( !Networking.IsOwner(gameObject) ) return;
        goValue = Mathf.Max(Mathf.Min(goValue+1, gosyss.Length/2),0);
        RequestSerialization();
    }

    public void SubGo()
    {
        if ( !Networking.IsOwner(gameObject) ) return;
        goValue = Mathf.Max(Mathf.Min(goValue-1, gosyss.Length/2),0);
        RequestSerialization();
    }
    
    void Start()
    {
        if ( !Networking.IsOwner(gameObject) ) return;
        goValue = 1;
        RequestSerialization();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        
    }

    public void SimulOn()
    {
/*        foreach (GoSystem gs in gosyss) {
            Debug.Log("status: "+gs.status);
            if( gs.status != GoSystemStatus.Standby ) return;
        }
*/
        for (int i=0; i<gosyss.Length; i++) {
            int h = gosyss.Length / 2;
            float w = 1.5f;
            gosyss[i].gameObject.transform.position = new Vector3(-h*w+i*w, 0, 0);
            playAreas[i].gameObject.transform.rotation = Quaternion.Euler(0,90,0);
            cameras[i].gameObject.transform.rotation = Quaternion.Euler(90,180,0);
            screens[i].gameObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }
    }

    public void SimulOff() {
//        foreach (GoSystem gs in gosyss) if( gs.status != GoSystemStatus.Standby ) return;

        for (int i=0; i<gosyss.Length; i++) {
            GoSystem gosys = gosyss[i];
            int h = gosyss.Length / 2;
            float w = 5.0f;
            gosyss[i].gameObject.transform.position = new Vector3(-h*w+i*w, 0, 0);
            playAreas[i].gameObject.transform.rotation = Quaternion.Euler(0,0,0);
            cameras[i].gameObject.transform.rotation = Quaternion.Euler(90,90,0);
            screens[i].gameObject.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
        }
    }
}
