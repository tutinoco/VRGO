using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public enum GoSystemStatus
{
    Standby,
    Playing,
    Kento,
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GoSystem : UdonSharpBehaviour
{
    [Header("管理する碁石を設定します")]
    [SerializeField] private VRCObjectPool blackPool;
    [SerializeField] private VRCObjectPool whitePool;
    private Stone[] blacks;
    private Stone[] whites;

    [Header("碁盤の基本情報を設定します")]    
    [SerializeField] private float roWidth;
    [SerializeField] private float roHeight;
    [SerializeField] private int roNumber;    

    [Header("SGFの出力先となるInputFieldを設定します")]
    [SerializeField] private InputField sgfField;

    [Header("プレイヤー名を表示するTextを設定します")]
    [SerializeField] private Text blackUserText;
    [SerializeField] private Text whiteUserText;

    [Header("碁石を一時的に隠しておく場所HiddenBoxを設定します")]
    [SerializeField] private GameObject hiddenBox;

    [Header("プレイエリアを設定します")]
    [SerializeField] public GameObject playArea;

    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(log))] private string _log = "";
    [UdonSynced(UdonSyncMode.None)] private int pcnt;
    private Stone[] logSt = new Stone[0];
    private Vector3[] logPt = new Vector3[0];

    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(blackUser))] private int _blackUser;
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(whiteUser))] private int _whiteUser;

    [System.NonSerialized] public bool isNormal;
    [System.NonSerialized] public bool isMark;
    [System.NonSerialized] public GoSystemStatus status = GoSystemStatus.Standby;

    private Stone tgtStone;

    public void Resync() { ReadLog(pcnt); }

    public void PrevLog() { if( pcnt < logSt.Length-1 ) ReadLog(pcnt+1); }
    public void NextLog() { if( pcnt >= 0 ) ReadLog(pcnt-1); }
    public void FirstLog() { if( pcnt < logSt.Length-1 ) ReadLog(logSt.Length-1); }
    public void LastLog() { if( pcnt > 0 ) ReadLog(-1); }

    public void Set19Ro() { roNumber = 19; }
    public void Set13Ro() { roNumber = 13; }
    public void Set9Ro() { roNumber = 9; }

    public void NormalOn() { isNormal = true; }
    public void NormalOff() { isNormal = false; }

    public void MarkOn() { isMark = true; UpdateMark(); }
    public void MarkOff() { isMark = false; AllMarkerOff(); }

    public void KentoOn() { if ( logSt.Length > 0 ) { status=GoSystemStatus.Kento; ReadLog(pcnt=-1); sgfField.text=GetSgf(); } }
    public void KentoOff() { status=GoSystemStatus.Playing; ReadLog(pcnt=-1); }

    public string log {
        set {
            _log = value;

            // 軽量化のためログが更新されたら石と座標データを取得
            string[] lines = value.Split('\n');
            if ( lines[0] == "" ) return;
            int max = 0;
            logSt = new Stone[lines.Length];
            logPt = new Vector3[lines.Length];
            foreach (string line in lines) {
                string[] data = line.Split(',');
                int idx = int.Parse(data[0].Substring(1));
                bool isBlack = data[0].Substring(0,1) == "B";
                GameObject g = isBlack ? blackPool.Pool[idx] : whitePool.Pool[idx];
                if ( g != null ) {
                    logSt[max] = (Stone)g.transform.GetComponent(typeof(UdonBehaviour));
                    logPt[max] = new Vector3(float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]));
                    max++;
                }
            }

            // ログが更新されたら最終手にマーカーをつける
            if(isMark) UpdateMark();
        }
        get { return _log; }
    }

    public int blackUser {
        set { _blackUser = value; ShowUserName(true, value); }
        get { return _blackUser; }
    }

    public int whiteUser {
        set { _whiteUser = value; ShowUserName(false, value); }
        get { return _whiteUser; }
    }

    void Start()
    {
        blacks = new Stone[blackPool.Pool.Length];
        whites = new Stone[whitePool.Pool.Length];

        for (int i=0; i<blackPool.Pool.Length; i++) {
            Stone s = (Stone)blackPool.Pool[i].transform.GetComponent(typeof(UdonBehaviour));
            s.idx = i;
            s.isBlack = true;
            blacks[i] = s;
        }

        for (int i=0; i<whitePool.Pool.Length; i++) {
            Stone s = (Stone)whitePool.Pool[i].transform.GetComponent(typeof(UdonBehaviour));
            s.idx = i;
            s.isBlack = false;
            whites[i] = s;
        }
    }

    void Update()
    {
        if ( Networking.IsOwner(gameObject) ) TryToSpawn();
        //HogeHoge();
    }

    private void HogeHoge()
    {
        VRCPlayerApi player = Networking.LocalPlayer;
//        if ( !player.IsUserInVR() ) return;

        Vector3 p = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
        RaycastHit hit;
        Ray ray = new Ray(p, new Vector3(0, -1, 0));
        if ( Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 22) ) {
            Debug.DrawRay(hit.point, hit.normal, Color.green);//            if( hit != null ) tgtGobj.transform.position = hit.point;
        }

        /*
        bool e = false;
        Vector3 p = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
        RaycastHit hit;
        Ray ray = new Ray(p, new Vector3(0, -1, 0));
        if ( Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 22) ) {
            if( hit.collider == null ) e = true;
            else {
                Stone s = (Stone)hit.collider.gameObject.transform.GetComponent(typeof(UdonBehaviour));
                s.onHandAreaEnter(p);
                if( tgtStone != s ) e = true;

                tgtGobj.transform.position = hit.point;
            }

            if ( e && tgtStone!=null ) {
                tgtStone.onHandAreaExit();
                tgtStone = null;
            }
        }
        */
    }

    private void TryToSpawn()
    {
        RaycastHit hit;
        Vector3 p = blackPool.gameObject.transform.position;
        Ray ray = new Ray(new Vector3(p.x, p.y+0.5f, p.z), new Vector3(0, -1, 0));
        if ( Physics.Raycast(ray, out hit) && hit.collider!=null && hit.collider.gameObject.layer==24 ) blackPool.TryToSpawn();
        p = whitePool.gameObject.transform.position;
        ray = new Ray(new Vector3(p.x, p.y+0.5f, p.z), new Vector3(0, -1, 0));
        if ( Physics.Raycast(ray, out hit) && hit.collider!=null && hit.collider.gameObject.layer==24 ) whitePool.TryToSpawn();
    }

    public void Return( Stone s )
    {
        s.transform.localPosition = GetSpawnPoint(s.isBlack);
        s.transform.rotation = Quaternion.Euler(-90, UnityEngine.Random.Range(0f,360f), 0);
    }

    private Vector3 GetSpawnPoint( bool isBlack )
    {
        Vector3 p = isBlack ? blackPool.gameObject.transform.localPosition : whitePool.gameObject.transform.localPosition;
        return new Vector3(p.x+UnityEngine.Random.Range(-0.001f,0.001f), p.y, p.z+UnityEngine.Random.Range(-0.001f,0.001f));
    }

    public void Reset()
    {
        // 全ての権限を移譲する
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        Networking.SetOwner(Networking.LocalPlayer, blackPool.gameObject);
        Networking.SetOwner(Networking.LocalPlayer, whitePool.gameObject);
        for (int i=0; i<blacks.Length; i++) Networking.SetOwner(Networking.LocalPlayer, blacks[i].gameObject);
        for (int i=0; i<whites.Length; i++) Networking.SetOwner(Networking.LocalPlayer, whites[i].gameObject);
        foreach (Stone s in whites) {
            GameObject g = s.gameObject;
            Networking.SetOwner(Networking.LocalPlayer, g);
            if( g.activeSelf ) whitePool.Return( g );
        }
        foreach (Stone s in blacks) {
            GameObject g = s.gameObject;
            Networking.SetOwner(Networking.LocalPlayer, g);
            if( g.activeSelf ) blackPool.Return( g );
        }

        log = "";
        pcnt = 0;
        blackUser = 0;
        whiteUser = 0;
        logSt = new Stone[0];
        logPt = new Vector3[0];   
        status = GoSystemStatus.Standby;

        RequestSerialization();
    }

    private void UpdateMark()
    {
        if ( logSt.Length == 0 ) return;
        AllMarkerOff();
        logSt[0].MarkerOn();
    }

    public void AllMarkerOff()
    {
        foreach (Stone s in blacks) if( s.gameObject.activeSelf ) s.MarkerOff();
        foreach (Stone s in whites) if( s.gameObject.activeSelf ) s.MarkerOff();
    }

    public Vector2Int GetZahyo( Vector3 pos )
    {
        return new Vector2Int((int)Mathf.Round(pos.x/roWidth),(int)Mathf.Round(pos.z/roHeight));
    }

    public Vector3 GetNormalPosition( Vector3 pos )
    {
        Vector2Int zahyo = GetZahyo(pos);
        return new Vector3((float)zahyo.x*roWidth, pos.y, (float)zahyo.y*roHeight);
    }

    private void ShowUserName( bool isBlack, int playerId )
    {
        string name = playerId>0 ? VRCPlayerApi.GetPlayerById(playerId).displayName : "";
        Text t = isBlack ? blackUserText : whiteUserText;
        String s = (isBlack?"黒":"白")+"\n<size=36>"+name+"</size>";
        t.text = name=="" ? "" : s;
    }

    public void WriteLog( Stone s )
    {
        if( status == GoSystemStatus.Standby ) status = GoSystemStatus.Playing;

        // 初手のユーザ名を記録
        int playerId = Networking.GetOwner(s.gameObject).playerId;
        if( s.isBlack && blackUser==0 ) blackUser = playerId;
        if( !s.isBlack && whiteUser==0 ) whiteUser = playerId;

        if( logSt.Length > 0 ) {

            // 最後手が再配置されたら書換のため最後のログを削除
            if( s == logSt[0] ) log = log.Substring(log.IndexOf('\n')+1);

            // 一度打たれた石が再度碁盤に配置されたときは記録しない
            bool isHit = false;
            for (int i=1; i<logSt.Length; i++) if ( s == logSt[i] ) { isHit = true; break; }
            if ( isHit ) {
                Vector2Int z = GetZahyo( s.gameObject.transform.localPosition );
                int h = (int)Mathf.Round(roNumber/2);
                if( z.x >= -h && z.x <= h && z.y >= -h && z.y <= h ) return;
            }
        }

        // ログを取る
        Vector3 pos = s.transform.localPosition;
        string line = (s.isBlack?"B":"W")+s.idx+','+pos.x+','+pos.y+','+pos.z;
        if ( log == "" ) log = line;
        else log = line+'\n'+log;

        // 全てのPCでログを更新
        RequestSerialization();
    }

    private void ReadLog( int step )
    {
        Vector3[] bptAry = new Vector3[blacks.Length];
        Vector3[] wptAry = new Vector3[whites.Length];
        bool[] bReturns = new bool[blacks.Length];
        bool[] wReturns = new bool[whites.Length];

        if( isMark ) SendCustomNetworkEvent(NetworkEventTarget.All, "AllMarkerOff");

        Vector3 hiddenPoint = hiddenBox.transform.localPosition;
        for (int i=0; i<blacks.Length; i++) if ( blacks[i].gameObject.activeSelf ) { bptAry[i]=hiddenPoint; /*bReturns[i]=true;*/ }
        for (int i=0; i<whites.Length; i++) if ( whites[i].gameObject.activeSelf ) { wptAry[i]=hiddenPoint; /*wReturns[i]=true;*/ }

/*
        for (int i=0; i<logSt.Length; i++) { Stone s=logSt[i]; if(s.isBlack) bReturns[s.idx]=false; else wReturns[s.idx]=false; }
        for (int i=0; i<bReturns.Length; i++) if( bReturns[i] ) { blackPool.Return(blacks[i].gameObject); bptAry[i]=(new Vector3[1])[0]; }
        for (int i=0; i<wReturns.Length; i++) if( wReturns[i] ) { whitePool.Return(whites[i].gameObject); wptAry[i]=(new Vector3[1])[0]; }
*/
        for (int i=logSt.Length-1; i>step; i--) {
            Stone s = logSt[i];
            Vector3 p = logPt[i];
            if( s.isBlack ) bptAry[s.idx] = p; else wptAry[s.idx] = p;
            if( isMark && i==step+1 ) s.SendCustomNetworkEvent(NetworkEventTarget.All, "MarkerOn");
        }

        for (int i=0; i<bptAry.Length; i++) {
            Vector3 p1 = bptAry[i];
            if ( p1 == null ) continue;
            Stone s = blacks[i];
            Vector3 p2 = s.transform.localPosition;
            if ( Mathf.Abs(p1.x-p2.x) < roWidth*0.1 && Mathf.Abs(p1.z-p2.z) < roHeight*0.1 ) continue; // 位置が変わっていなければ無視
            Networking.SetOwner(Networking.LocalPlayer, s.gameObject);
            s.TeleportTo(p1);
        }

        for (int i=0; i<wptAry.Length; i++) {
            Vector3 p1 = wptAry[i];
            if ( p1 == null ) continue;
            Stone s = whites[i];
            Vector3 p2 = s.transform.localPosition;
            if ( Mathf.Abs(p1.x-p2.x) < roWidth*0.1 && Mathf.Abs(p1.z-p2.z) < roHeight*0.1 ) continue; // 位置が変わっていなければ無視
            Networking.SetOwner(Networking.LocalPlayer, s.gameObject);
            s.TeleportTo(p1);
        }

        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        pcnt = step;
        RequestSerialization();
    }

    public string GetSgf()
    {
        DateTime dt = DateTime.Now;
        string blackName = blackUser > 0 ? VRCPlayerApi.GetPlayerById(blackUser).displayName : "NO NAME";
        string whiteName = whiteUser > 0 ? VRCPlayerApi.GetPlayerById(whiteUser).displayName : "NO NAME";
        string sgf = "(;AP[VRGO:1.0]SZ["+roNumber+"]PB["+blackName+"]PW["+whiteName+"]KM[6.5]DT["+dt.Year.ToString()+"-"+dt.Month.ToString()+"-"+dt.Day.ToString()+"]";

        bool prevIsBlack = false;
        for (int i=logSt.Length-1; i>=0; i--) {
            Stone s = logSt[i];
            Vector2Int zahyo = GetZahyo(logPt[i]);
            int h = (int)Mathf.Round(roNumber/2);
            if ( Mathf.Abs(zahyo.x) > h || Mathf.Abs(zahyo.y) > h ) continue; // 路の外（アゲハマなど）の座標を無視
            if( prevIsBlack == s.isBlack ) sgf += ";"+(s.isBlack?"W":"B")+"[tt]"; // 前の手が同じ色の石ならパスを挿入
            prevIsBlack = s.isBlack;
            char x = (char)(97+h - zahyo.x);
            char y = (char)(97+h - zahyo.y);
            sgf += ";"+(s.isBlack?"B":"W")+"["+x+y+"]";
        }
        
        sgf += ")";
        return sgf;
    }
}
