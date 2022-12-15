using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("碁盤の基本情報を設定します")]    
    [SerializeField] private float roWidth;
    [SerializeField] private float roHeight;
    [SerializeField] private int roNumber;    

    [Header("SGFの出力先となるInputFieldを設定します")]    
    [SerializeField] private InputField sgfField;

    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(log))] private string _log = "";
    [UdonSynced(UdonSyncMode.None)] private int pcnt;
    [UdonSynced(UdonSyncMode.None)] private string blackUser = "";
    [UdonSynced(UdonSyncMode.None)] private string whiteUser = "";

    [System.NonSerialized] public bool isNormal;
    [System.NonSerialized] public bool isMark;
    [System.NonSerialized] public bool isKento;

    public string log {
        set { _log = value; if(isMark) UpdateMark(); }
        get { return _log; }
    }

    public void Respawn( Stone s )
    {
        s.transform.localPosition = GetRespawnPosition(s);
        s.transform.rotation = Quaternion.Euler(-90, UnityEngine.Random.Range(0f,360f), 0);
    }

    private Vector3 GetRespawnPosition( Stone s )
    {
        Vector3 p = s.isBlack ? blackPool.gameObject.transform.position : whitePool.gameObject.transform.position;
        return new Vector3(p.x+UnityEngine.Random.Range(-0.001f,0.001f), 0, p.z+UnityEngine.Random.Range(-0.001f,0.001f));
    }

    void Update()
    {
        if ( Networking.IsOwner(gameObject) ) TryToSpawn();
    }

    private void UpdateMark()
    {
        string id = GetLastId();
        if( id != "" ) FindStone(id).MarkerOn();
    }

    private string GetLastId()
    {
        string[] lines = log.Split('\n');
        if ( lines[0] == "" ) return "";
        string[] data = lines[0].Split(',');
        return data[0];
    }

    private string GetId( Stone s )
    {
        return (s.isBlack ? "B" : "W") + FindIndex(s);
    }

    private int CountLog( Stone s )
    {
        int count = 0;
        string[] lines = log.Split('\n');
        if ( lines[0] == "" ) return 0;
        string id1 = GetId(s);
        foreach (string line in lines) {
            string id2 = line.Split(',')[0];
            if ( id1 == id2 ) count++;
        }
        return count;
    }

    private void TryToSpawn()
    {
        RaycastHit hit;
        Vector3 p = blackPool.gameObject.transform.position;
        Ray ray = new Ray(new Vector3(p.x, p.y+0.5f, p.z), new Vector3(0, -1, 0));
        if ( Physics.Raycast(ray, out hit) && hit.collider!=null && hit.collider.gameObject.layer==24 ) SpawnBlack();
        p = whitePool.gameObject.transform.position;
        ray = new Ray(new Vector3(p.x, p.y+0.5f, p.z), new Vector3(0, -1, 0));
        if ( Physics.Raycast(ray, out hit) && hit.collider!=null && hit.collider.gameObject.layer==24 ) SpawnWhite();
    }

    public void SetOwnerAll()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        Networking.SetOwner(Networking.LocalPlayer, blackPool.gameObject);
        Networking.SetOwner(Networking.LocalPlayer, whitePool.gameObject);
        for (int i=0; i<blackPool.Pool.Length; i++) Networking.SetOwner(Networking.LocalPlayer, blackPool.Pool[i]);
        for (int i=0; i<whitePool.Pool.Length; i++) Networking.SetOwner(Networking.LocalPlayer, whitePool.Pool[i]);
    }

    public void Reset()
    {
        // 全てのPCで全ての石を戻す
        SetOwnerAll();
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Initialize));
    }

    public void AllMarkerOff()
    {
        for (int i=0; i<blackPool.Pool.Length; i++) {
            Stone s = (Stone)blackPool.Pool[i].transform.GetComponent(typeof(UdonBehaviour));
            s.MarkerOff();
        }
        for (int i=0; i<whitePool.Pool.Length; i++) {
            Stone s = (Stone)whitePool.Pool[i].transform.GetComponent(typeof(UdonBehaviour));
            s.MarkerOff();
        }
    }

    public void Initialize()
    {
        log = "";
        pcnt = 0;
        blackUser = "";
        whiteUser = "";
        for (int i=0; i<blackPool.Pool.Length; i++) blackPool.Return( blackPool.Pool[i] );
        for (int i=0; i<whitePool.Pool.Length; i++) whitePool.Return( whitePool.Pool[i] );
    }

    public Stone SpawnBlack()
    {
        GameObject g = blackPool.TryToSpawn();
        if ( !g ) return null;
        Stone s = (Stone)g.transform.GetComponent(typeof(UdonBehaviour));
        return s;
    }

    public Stone SpawnWhite()
    {
        GameObject g = whitePool.TryToSpawn();
        if ( !g ) return null;
        Stone s = (Stone)g.transform.GetComponent(typeof(UdonBehaviour));
        return s;
    }

    public Stone FindStone( string id )
    {
        bool isBlack = id.Substring(0,1) == "B";
        int i = int.Parse(id.Substring(1));
        GameObject g = null;
        g = isBlack ? blackPool.Pool[i] : whitePool.Pool[i];
        return g == null ? null : (Stone)g.transform.GetComponent(typeof(UdonBehaviour));
    }

    public int FindIndex( Stone s )
    {
        VRCObjectPool pool = s.isBlack ? blackPool : whitePool;
        for (int i=0; i<pool.Pool.Length; i++) if ( s.gameObject == pool.Pool[i] ) return i;
        return -1;
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

    public void WriteLog( Stone s )
    {
        if( s.isBlack && blackUser=="" ) blackUser = Networking.GetOwner(s.gameObject).displayName;
        if( !s.isBlack && whiteUser=="" ) whiteUser = Networking.GetOwner(s.gameObject).displayName;

        string id = GetId(s);
        if( id == GetLastId() ) log = log.Substring(log.IndexOf('\n')+1); // 最後手が再配置されたら書換のため最後のログを削除
        Vector2Int z = GetZahyo( s.gameObject.transform.localPosition );

        // 一度打たれた石が再度碁盤に配置されたときは記録しない
        int h = (int)Mathf.Round(roNumber/2);
        if( CountLog(s) > 0 && z.x >= -h && z.x <= h && z.y >= -h && z.y <= h ) {
//            s.gameObject.transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f); 打たれた石が別の場所に配置されたときに警告
            return;
        }

        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateMark));

        Vector3 pos = s.transform.localPosition;
        string line = id+','+pos.x+','+pos.y+','+pos.z;
        if ( log == "" ) log = line;
        else log = line+'\n'+log;
        RequestSerialization();
    }

    public string GetSgf()
    {
        DateTime dt = DateTime.Now;
        string sgf = "(;AP[VRGO:1.0]SZ["+roNumber+"]PB["+blackUser+"]PW["+whiteUser+"]KM[6.5]DT["+dt.Year.ToString()+"-"+dt.Month.ToString()+"-"+dt.Day.ToString()+"]";

        string prevType = "W";
        string[] lines = log.Split('\n');
        for (int i=GetLogLength()-1; i>=0; i--) {
            string line = lines[i];
            string[] data = line.Split(',');
            string id = data[0];
            string type = id.Substring(0,1);
            Vector2Int zahyo = GetZahyo(new Vector3(float.Parse(data[1]), 0, float.Parse(data[3])));
            int h = (int)Mathf.Round(roNumber/2);
            if ( Mathf.Abs(zahyo.x) > h || Mathf.Abs(zahyo.y) > h ) continue;
            if( prevType == type ) sgf += ";"+(type=="B"?"W":"B")+"[tt]";
            prevType = type;
            char x = (char)(97+h - zahyo.x);
            char y = (char)(97+h - zahyo.y);
            sgf += ";"+type+"["+x+y+"]";
        }
        
        sgf += ")";
        return sgf;
    }

    private void ReadLog( int step )
    {
        string[] lines = log.Split('\n');
        Stone[] logSt = new Stone[lines.Length];
        Vector3[] logPs = new Vector3[lines.Length];
        Vector3[] blacks = new Vector3[blackPool.Pool.Length];
        Vector3[] whites = new Vector3[whitePool.Pool.Length];

        if( isMark ) SendCustomNetworkEvent(NetworkEventTarget.All, "AllMarkerOff");

        for (int i=0; i<blackPool.Pool.Length; i++) {
            Stone s = (Stone)blackPool.Pool[i].transform.GetComponent(typeof(UdonBehaviour));
            if ( s.gameObject.activeSelf ) blacks[i] = GetRespawnPosition(s);
        }

        for (int i=0; i<whitePool.Pool.Length; i++) {
            Stone s = (Stone)whitePool.Pool[i].transform.GetComponent(typeof(UdonBehaviour));
            if ( s.gameObject.activeSelf ) whites[i] = GetRespawnPosition(s);
        }

        for (int i=lines.Length-1; i>step; i--) {
            string line = lines[i];
            string[] data = line.Split(',');
            Stone s = FindStone(data[0]);
            Vector3 p = new Vector3(float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]));

            if( s.isBlack ) blacks[FindIndex(s)] = p; else whites[FindIndex(s)] = p;
            if( i==step && isMark ) s.SendCustomNetworkEvent(NetworkEventTarget.All, "MarkerOn");
        }

        foreach (Vector3[] stones in new Vector3[][] { blacks, whites }) {
            for (int i=0; i<stones.Length; i++) {
                Vector3 p1 = stones[i];
                if ( p1 == null ) continue;
                string id = (stones==blacks?"B":"W") + i;
                Stone s = FindStone(id);
                Vector3 p2 = s.transform.localPosition;
                if ( Mathf.Abs(p1.x-p2.x) < roWidth*0.1 && Mathf.Abs(p1.z-p2.z) < roHeight*0.1 ) continue;
                Networking.SetOwner(Networking.LocalPlayer, s.gameObject);
                s.TeleportTo(p1);
            }
        }

        pcnt = step;
        RequestSerialization();
    }

    public void PrevLog()
    {
        if( pcnt >= GetLogLength()-1 ) return;
        ReadLog(pcnt+1);
    }

    public void NextLog() { if( pcnt >= 0 ) ReadLog(pcnt-1); }

    public int GetLogLength() { return log.Split('\n').Length; }
    public void Resync() { ReadLog(pcnt); }
    public void Set19Ro() { roNumber = 19; }
    public void Set13Ro() { roNumber = 13; }
    public void Set9Ro() { roNumber = 9; }
    public void NormalOn() { isNormal = true; }
    public void NormalOff() { isNormal = false; }
    public void MarkOn() { isMark = true; UpdateMark(); }
    public void MarkOff() { isMark = false; AllMarkerOff(); }
    public void KentoOn() { if ( log.Length > 0 ) { isKento = true; ReadLog(pcnt=-1); sgfField.text=GetSgf(); } }
    public void KentoOff() { isKento = false; ReadLog(pcnt=0); }
}
