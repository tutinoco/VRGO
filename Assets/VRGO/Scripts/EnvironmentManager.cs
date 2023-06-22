using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class EnvironmentManager : UdonSharpBehaviour
{
    [Header("管理するGoManagerを設定します")]
    [SerializeField] private GameObject goSystemsGobj;
    [SerializeField] private GameObject screensGobj;

    [Header("Masterの足元に配置されるGameObjectを設定します")]
    [SerializeField] private GameObject masterPlane;

    [Header("EnvironmentPaneのGameObjectを設定します")]
    [SerializeField] private GameObject environmentPane;

    private GoSystem[] goSystems;
    private GameObject[] playAreas;
    private GameObject[] screens;

    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(goValue))] private int _goValue;
    public int goValue {
        set {
            _goValue = value;
            int h = goSystems.Length / 2;
            for (int i=0; i<goSystems.Length; i++) {
                bool b = i >= h-_goValue && i <= h+_goValue;
                goSystems[i].gameObject.SetActive(b);
            }
        }
        get { return _goValue; }
    }

    void Start()
    {
        int len1 = goSystemsGobj.transform.childCount;
        goSystems = new GoSystem[len1];
        playAreas = new GameObject[len1];
        for (int i=0; i<len1; i++) {
            goSystems[i] = (GoSystem)goSystemsGobj.transform.GetChild(i).GetComponent(typeof(UdonBehaviour));
            playAreas[i] = goSystems[i].transform.Find("PlayArea").gameObject;

            for (int j=0; j<2; j++) {
                GameObject alphabet = playAreas[i].transform.Find("ScreenTarget").transform.Find("TableChar").gameObject; 
                Text t = alphabet.transform.GetChild(j).GetComponent<Text>();

                string tablechar = "いろはにほへとちりぬるをわかよたれそつねならむうゐのおくやまけふこえてあさきゆめみしゑひもせすん";
                t.text = tablechar[Mathf.Abs(len1-1-i*2+(len1/2<i?1:0))]+"";
            }
        }

        int len2 = screensGobj.transform.childCount;
        screens = new GameObject[len2];
        for (int i=0; i<len2; i++) {
            GameObject screen = screensGobj.transform.GetChild(0).gameObject; // SetParentの影響でunshiftする形となるためインデックスは0になります。
            screens[i] = screen.transform.Find("Screen").gameObject;
            screen.transform.SetParent(goSystems[i].gameObject.transform);
            screen.transform.Find("Camera").SetParent(playAreas[i].transform);
            goSystems[i].gameObject.transform.Find("PlayerNames").SetParent(screens[i].transform);
            goSystems[i].gameObject.transform.Find("Time").SetParent(screens[i].transform);
        }

        if ( !Networking.IsOwner(gameObject) ) return;
        goValue = 3;
        RequestSerialization();
    }

    private void Update()
    {
        environmentPane.SetActive(Networking.IsMaster);

        var player = Networking.LocalPlayer;
        if (Networking.IsMaster) {
            Vector3 p = player.GetPosition();
            masterPlane.transform.position = new Vector3(p.x, 0.001f, p.z);
        }

        // 掴みながらShift押すとゆっくり移動するように変更
        var rightHand = player.GetPickupInHand(VRC_Pickup.PickupHand.Right);
        var leftHand = player.GetPickupInHand(VRC_Pickup.PickupHand.Left);
        bool isPickup = rightHand != null || leftHand != null;
        if ( isPickup && Input.GetKey(KeyCode.LeftShift) ) {
            player.SetRunSpeed(0.5f);
            player.SetWalkSpeed(0.5f);
            player.SetStrafeSpeed(0.5f);
        } else {
            player.SetRunSpeed(4.0f);
            player.SetWalkSpeed(2.0f);
            player.SetStrafeSpeed(2.0f);
        }
    }

    public void AddGo()
    {
        if ( !Networking.IsOwner(gameObject) ) return;
        goValue = Mathf.Max(Mathf.Min(goValue+1, goSystems.Length/2),0);
        RequestSerialization();
    }

    public void SubGo()
    {
        if ( !Networking.IsOwner(gameObject) ) return;
        goValue = Mathf.Max(Mathf.Min(goValue-1, goSystems.Length/2),0);
        RequestSerialization();
    }
    
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        
    }

    public void SimulOn()
    {
/*        foreach (GoSystem gs in goSystems) {
            Debug.Log("status: "+gs.status);
            if( gs.status != goSystemsGobjtatus.Standby ) return;
        }
*/
        for (int i=0; i<goSystems.Length; i++) {
            int h = goSystems.Length / 2;
            float w = 1.5f;
            goSystems[i].gameObject.transform.localPosition = new Vector3(-h*w+i*w, 0, 0);
            playAreas[i].gameObject.transform.localRotation = Quaternion.Euler(0,90,0);
            screens[i].gameObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }
    }

    public void SimulOff() {
//        foreach (GoSystem gs in goSystems) if( gs.status != goSystemsGobjtatus.Standby ) return;

        for (int i=0; i<goSystems.Length; i++) {
            GoSystem gosys = goSystems[i];
            int h = goSystems.Length / 2;
            float w = 5.0f;
            goSystems[i].gameObject.transform.localPosition = new Vector3(-h*w+i*w, 0, 0);
            playAreas[i].gameObject.transform.localRotation = Quaternion.Euler(0,0,0);
            screens[i].gameObject.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
        }
    }
}
