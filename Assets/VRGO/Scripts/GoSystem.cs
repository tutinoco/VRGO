﻿using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using tutinoco;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GoSystem : UdonSharpBehaviour
{
    [Header("管理する碁石を設定します")]
    [SerializeField] private VRCObjectPool blackPool;
    [SerializeField] private VRCObjectPool whitePool;
    private Stone[] blacks;
    private Stone[] whites;

    [Header("碁盤の基本情報を設定します")]    
    [SerializeField] private int roNumber;
    [SerializeField] private float roWidth;
    [SerializeField] private float roHeight;
    [SerializeField] private float boardHeight;

    [Header("SGFの入出力先となるInputFieldを設定します")]
    [SerializeField] private InputField sgfInputField;
    [SerializeField] private InputField sgfOutputField;

    [Header("プレイ時間を表示するTextを設定します")]
    [SerializeField] private Text playTimeText;

    [Header("コミを表示するTextを設定します")]
    [SerializeField] private Text komiText;

    [Header("プレイヤー名を表示するTextを設定します")]
    [SerializeField] private Text blackUserText;
    [SerializeField] private Text whiteUserText;

    [Header("システムが利用するスイッチを登録します")]
    [SerializeField] private BenriSwitch GoSystemPaneSwitch;
    [SerializeField] private BenriSwitch kentoSwitch;

    [Header("最終手を知らせるマーカーのGameObjectを登録します")]
    [SerializeField] private GameObject Mark;

    [Header("碁石に触られないようにするガードのGameObjectを登録します")]
    [SerializeField] private GameObject Guard;

    [Header("碁石の位置をわかりやすくするガイドのGameObjectを登録します")]
    [SerializeField] private GameObject Guide;
    private Stone GuideTargetStone;

    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(log))] private string _log = "";
    [UdonSynced(UdonSyncMode.None)] private int pcnt = -1;
    private Stone[] logSt = new Stone[0];
    private Vector3[] logPt = new Vector3[0];

    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(blackUser))] private string _blackUser;
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(whiteUser))] private string _whiteUser;
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(komi))] private int _komi = 3;
    [UdonSynced(UdonSyncMode.None)] private double startTime;

    [System.NonSerialized] public bool isNormal;
    [System.NonSerialized] public bool isKento;

    public void SetGuideTargetStone( Stone s ) { GuideTargetStone = s; }

    public void PrevLog() { if( pcnt < logSt.Length-1 ) ReadLog(pcnt+1); }
    public void NextLog() { if( pcnt >= 0 ) ReadLog(pcnt-1); }
    public void FirstLog() { if( pcnt < logSt.Length-1 ) ReadLog(logSt.Length-1); }
    public void LastLog() { if( pcnt > 0 ) ReadLog(-1); }

    public void Set19Ro() { roNumber = 19; }
    public void Set13Ro() { roNumber = 13; }
    public void Set9Ro() { roNumber = 9; }

    public void NormalOn() { isNormal = true; }
    public void NormalOff() { isNormal = false; }

    public void KentoOn() { if ( logSt.Length > 0 ) { isKento = true; ReadLog(pcnt=-1); } }
    public void KentoOff() { isKento = false; ReadLog(pcnt=-1); }

    public void SpawnBlack() { if ( Networking.IsOwner(blackPool.gameObject) ) blackPool.TryToSpawn(); }
    public void SpawnWhite() { if ( Networking.IsOwner(whitePool.gameObject) ) whitePool.TryToSpawn(); }

    public void GoSystemPaneOpen() { GoSystemPane(true); }
    public void GoSystemPaneClose() { GoSystemPane(false); }

    public void KomiAdd() { if ( !(komi >= 4) ) { komi++; RequestSerialization(); } }
    public void KomiSub() { if ( !(komi <= 0) ) { komi--; RequestSerialization(); } }

    public void GuardOn() { Guard.SetActive(true); string n=Networking.LocalPlayer.displayName; bool b=(Networking.LocalPlayer.isMaster||n==blackUser||n==whiteUser); Guard.GetComponent<Collider>().enabled=!b; }
    public void GuardOff() { Guard.SetActive(false); }

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
        }
        get { return _log; }
    }

    public string blackUser {
        set { _blackUser = value; ShowUserName(true, value); }
        get { return _blackUser; }
    }

    public string whiteUser {
        set { _whiteUser = value; ShowUserName(false, value); }
        get { return _whiteUser; }
    }

    public int komi {
        set {
            _komi = value;
            String[] t = new String[]{"<size=14>置碁</size>","コミ<size=14>なし</size>","コミ<size=18>5.5</size>","コミ<size=18>6.5</size>","コミ<size=18>7.5</size>"};
            komiText.text = t[value];
        }
        get { return _komi; }
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

        SendCustomEventDelayedSeconds(nameof(UpdateOneSeconds), 1.0f);
    }

    void Update()
    {
        if( GuideTargetStone != null ) {
            RaycastHit hit;
            int mask = ~(1 << 22 | 1 << 26);
            Ray ray = new Ray(GuideTargetStone.transform.position, new Vector3(0, -1, 0));
            if ( Physics.Raycast(ray, out hit, float.MaxValue, mask) && hit.collider!=null ) Guide.transform.position = hit.point;
        }

        Guard.transform.Rotate(0, 0, 2);

        Mark.gameObject.SetActive(false);
        if ( logSt.Length > 0 && pcnt+1 < logSt.Length ) {
            Mark.gameObject.SetActive(true);
            Vector3 p = logSt[pcnt+1].transform.position;
            Mark.transform.position = new Vector3(p.x, Mark.transform.position.y, p.z);
        }
    }

    public void UpdateOneSeconds()
    {
        TryToSpawn();
        UpdatePlayTime();
        SendCustomEventDelayedSeconds(nameof(UpdateOneSeconds), 1.0f);
    }

    public DateTime ToDatetime(double _unixTime)
    {
        DateTime ReturnTime = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        ReturnTime = ReturnTime.AddSeconds(_unixTime);
        return ReturnTime;
    }

    public double ToUnixDateTime(DateTime dateTime)
    {
        DateTime UnixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return (double)(dateTime - UnixTime).TotalSeconds;
    }

    public void UpdatePlayTime()
    {
        if ( isKento ) return;
        if ( startTime == 0 ) playTimeText.text = "";
        else {
            int totalSec = (int)(ToUnixDateTime(DateTime.Now) - startTime);
            int s = totalSec % 60;
            int m = totalSec / 60 % 60;
            int h = totalSec / 60 / 60;
            playTimeText.text = "プレイ時間 <size=38>"+h.ToString("00")+":"+m.ToString("00")+":"+s.ToString("00")+"</size>";
        }
    }

    private void TryToSpawn()
    {
        foreach (VRCObjectPool pool in new VRCObjectPool[]{ blackPool, whitePool } ) {
            if ( Networking.IsOwner(pool.gameObject) ) {
                Vector3 p = pool.gameObject.transform.position;
                RaycastHit hit;
                Ray ray = new Ray(new Vector3(p.x, p.y+0.03f, p.z), new Vector3(0, -1, 0));
                int mask = 1 << 22 | 1 << 24;
                if ( Physics.Raycast(ray, out hit, float.MaxValue, mask) && hit.collider!=null && hit.collider.gameObject.layer==24 ) pool.TryToSpawn();
            }
        }
    }

    public void Return( Stone s )
    {
        s.transform.localPosition = GetSpawnPoint(s.isBlack);
        s.transform.rotation = Quaternion.Euler(-90, UnityEngine.Random.Range(0f,360f), 0);
    }

    private Vector3 GetSpawnPoint( bool isBlack )
    {
        Vector3 p = isBlack ? blackPool.gameObject.transform.localPosition : whitePool.gameObject.transform.localPosition;
        return new Vector3(p.x+UnityEngine.Random.Range(-0.001f,0.001f), 0, p.z+UnityEngine.Random.Range(-0.001f,0.001f));
    }

    public void Reset()
    {
        // 検討モードをOFFにします。
        if ( kentoSwitch.isON ) kentoSwitch.Switch();

        // 全ての権限を移譲する
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        Networking.SetOwner(Networking.LocalPlayer, blackPool.gameObject);
        Networking.SetOwner(Networking.LocalPlayer, whitePool.gameObject);
        for (int i=0; i<blackPool.Pool.Length; i++) Networking.SetOwner(Networking.LocalPlayer, blackPool.Pool[i]);
        for (int i=0; i<whitePool.Pool.Length; i++) Networking.SetOwner(Networking.LocalPlayer, whitePool.Pool[i]);

        // 全てのPCで全ての石を戻す
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Initialize));
    }

    public void Initialize()
    {
        log = "";
        pcnt = -1;
        blackUser = "";
        whiteUser = "";
        startTime = 0;
        logSt = new Stone[0];
        logPt = new Vector3[0];

        if( !GoSystemPaneSwitch.isON ) GoSystemPaneSwitch.Switch();

        if ( Networking.IsOwner(blackPool.gameObject) ) foreach (Stone s in blacks) if( s.gameObject.activeSelf ) blackPool.Return( s.gameObject );
        if ( Networking.IsOwner(whitePool.gameObject) ) foreach (Stone s in whites) if( s.gameObject.activeSelf ) whitePool.Return( s.gameObject );
    }

    public Vector2Int GetZahyo( Vector3 pos )
    {
        return new Vector2Int((int)Mathf.Round(pos.x/roWidth),(int)Mathf.Round(pos.z/roHeight));
    }

    public void PutOnBoard( Stone s )
    {
        Vector3 p = s.transform.position;
        Vector3 a = s.transform.eulerAngles;
        s.transform.position = new Vector3(p.x, boardHeight, p.z);
        s.transform.rotation = Quaternion.Euler(-90, a.y, 0);
    }

    public Vector3 GetNormalPosition( Vector3 pos )
    {
        Vector2Int zahyo = GetZahyo(pos);
        foreach (Stone[] stones in new Stone[][]{ blacks, whites } ) {
            foreach (Stone s in stones) {
                if( !s.gameObject.activeSelf ) continue;
                Vector3 pos2 = s.transform.localPosition;
                if( pos == pos2 ) continue;
                Vector2Int zahyo2 = GetZahyo(pos2);
                if( zahyo.x == zahyo2.x && zahyo.y == zahyo2.y ) return pos;
            }
        }
        return new Vector3((float)zahyo.x*roWidth, pos.y, (float)zahyo.y*roHeight);
    }

    private void ShowUserName( bool isBlack, string name )
    {
        Text t = isBlack ? blackUserText : whiteUserText;
        String s = (isBlack?"黒":"白");
        t.text = name=="" ? s : s+": "+name;
    }

    public void GoSystemPane( bool open )
    {
        string localPlayerName = Networking.LocalPlayer.displayName;
        if( !(blackUser != "" && whiteUser != "" && !Networking.LocalPlayer.isMaster && !(localPlayerName==blackUser || localPlayerName==whiteUser)) ) {
            if( open != GoSystemPaneSwitch.isON ) GoSystemPaneSwitch.Switch();
        }
        GoSystemPaneSwitch.UpdateLinks();
    }

    public void WriteLog( Stone s )
    {
        // 初手のプレイヤーIDを記録
        int playerId = Networking.GetOwner(s.gameObject).playerId;
        if( startTime == 0 ) startTime = ToUnixDateTime(DateTime.Now);
        if( s.isBlack && blackUser=="" ) blackUser = VRCPlayerApi.GetPlayerById(playerId).displayName;
        if( !s.isBlack && whiteUser=="" ) whiteUser = VRCPlayerApi.GetPlayerById(playerId).displayName;

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
        string line = (s.isBlack?"B":"W")+s.idx+","+pos.x+","+pos.y+","+pos.z;
        log = log=="" ? line : line+'\n'+log;

        // 全てのPCでログを更新
        RequestSerialization();
    }

    private void ReadLog( int step )
    {
        Vector3[] bptAry = new Vector3[blacks.Length];
        Vector3[] wptAry = new Vector3[whites.Length];

        Vector3 blackSpawnPoint = GetSpawnPoint(true);
        Vector3 whiteSpawnPoint = GetSpawnPoint(false);
        for (int i=0; i<blacks.Length; i++) if ( blacks[i].gameObject.activeSelf ) bptAry[i] = blackSpawnPoint;
        for (int i=0; i<whites.Length; i++) if ( whites[i].gameObject.activeSelf ) wptAry[i] = whiteSpawnPoint;

        for (int i=logSt.Length-1; i>step; i--) {
            Stone s = logSt[i];
            Vector3 p = logPt[i];
            if( s.isBlack ) bptAry[s.idx] = p; else wptAry[s.idx] = p;
        }

        foreach (Vector3[] ptAry in new Vector3[][]{ bptAry, wptAry } ) {
            for (int i=0; i<ptAry.Length; i++) {
                Vector3 p1 = ptAry[i];
                if ( p1 == null ) continue;
                Stone s = ptAry==bptAry ? blacks[i] : whites[i];
                Vector3 p2 = s.transform.localPosition;
                if ( Mathf.Abs(p1.x-p2.x) < roWidth*0.1 && Mathf.Abs(p1.z-p2.z) < roHeight*0.1 ) continue; // 位置が変わっていなければ無視
                Networking.SetOwner(Networking.LocalPlayer, s.gameObject);
                s.TeleportTo(p1);
            }
        }

        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        pcnt = step;
        RequestSerialization();
    }

    private string getSgfValue(string sgf, int line, string name, string def)
    {
        sgf = sgf.Split(';')[line];
        int i = sgf.IndexOf(name);
        if ( i == -1 ) return def;
        i = sgf.IndexOf("[", i) + 1;
        return sgf.Substring(i, sgf.IndexOf("]",i)-i);
    }

    public void SetSgf()
    {
        Reset();

        string sgf = sgfInputField.text;
        int ro = int.Parse(getSgfValue(sgf,1,"SZ","19"));
        blackUser = getSgfValue(sgf,1,"PB","NO NAME");
        whiteUser = getSgfValue(sgf,1,"PW","NO NAME");

        string[] data = sgf.Split(';');

        if ( data.Length < 2 ) { sgfInputField.text = "ERROR!"; return; }

        for (int i=2; i<data.Length; i++) {
            char type = data[i][0];
            int h = (int)Mathf.Round(ro/2);
            float x = (data[i][2]-97-h) * roWidth;
            float y = (data[i][3]-97-h) * roHeight;

            Stone s = (Stone)(type=='B'?blackPool:whitePool).TryToSpawn().transform.GetComponent(typeof(UdonBehaviour));
            string line = type+""+s.idx+","+x+","+0+","+y;
            log = log=="" ? line : line+'\n'+log;
        }

        if ( !kentoSwitch.isON ) kentoSwitch.Switch();
        else ReadLog(pcnt=-1);
    }

    public void GetSgf()
    {
        DateTime dt = ToDatetime(startTime);
        string blackName = blackUser != "" ? blackUser : "NO NAME";
        string whiteName = whiteUser != "" ? whiteUser : "NO NAME";
        string k = (new String[]{"0","0","5.5","6.5","7.5"})[komi];
        string sgf = "(;AP[VRGO:2.2]SZ["+roNumber+"]PB["+blackName+"]PW["+whiteName+"]KM["+k+"]DT["+dt.Year.ToString()+"-"+dt.Month.ToString()+"-"+dt.Day.ToString()+"]";

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
        sgfOutputField.text = sgf;
    }
}
