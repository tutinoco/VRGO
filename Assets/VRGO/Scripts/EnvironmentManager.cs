using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EnvironmentManager : UdonSharpBehaviour
{
    [Header("管理するGoManagerを設定します")]
    [SerializeField] private GoSystem[] gosyss;
    [SerializeField] private GameObject[] screens;
    [SerializeField] private GameObject[] playAreas;
    [SerializeField] private GameObject[] cameras;

    private int goValue;

    public void AddGo() { SetGosysNumber(goValue+1); }
    public void SubGo() { SetGosysNumber(goValue-1); }
    
    void Start()
    {
        SetGosysNumber(1);
    }

    public void SimulOn()
    {
        for (int i=0; i<gosyss.Length; i++) {
            int h = gosyss.Length / 2;
            float w = 2.8f;
            gosyss[i].gameObject.transform.position = new Vector3(-h*w+i*w, 0, 0);
            playAreas[i].gameObject.transform.rotation = Quaternion.Euler(0,90,0);
            cameras[i].gameObject.transform.rotation = Quaternion.Euler(90,180,0);
            screens[i].gameObject.transform.position = new Vector3(-h*w+i*w, 0, 0);
        }
    }

    public void SimulOff() {
        for (int i=0; i<gosyss.Length; i++) {
            GoSystem gosys = gosyss[i];
            int h = gosyss.Length / 2;
            float w = 5.5f;
            gosyss[i].gameObject.transform.position = new Vector3(-h*w+i*w, 0, 0);
            playAreas[i].gameObject.transform.rotation = Quaternion.Euler(0,0,0);
            cameras[i].gameObject.transform.rotation = Quaternion.Euler(90,90,0);
            screens[i].gameObject.transform.position = new Vector3(-h*w+i*w, 0, 0);
        }
    }

    public void SetGosysNumber(int n) {
        int h = gosyss.Length / 2;
        n = Mathf.Min(n, h);
        if ( goValue == n ) return;
        int num = n * 2 + 1;

        for (int i=0; i<gosyss.Length; i++) {
            bool b = i >= h-n && i <= h+n;
            gosyss[i].gameObject.SetActive(b);
            screens[i].gameObject.SetActive(b);
        }
        goValue = n;
    }
}
