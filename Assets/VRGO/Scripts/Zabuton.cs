
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Zabuton : UdonSharpBehaviour
{
    [Header("目に見えるモデルを設定します")]
    [SerializeField] private GameObject model;
    
    void Start()
    {
    }

    void Update()
    {
        Vector3 p = gameObject.transform.localPosition;
        p = new Vector3(0,Mathf.Max(Mathf.Min(p.y,0.35f),-0.1f),0);
        model.gameObject.transform.localPosition = p;
        if ( Networking.IsOwner(gameObject) ) {
            gameObject.transform.localPosition = p;
            gameObject.transform.localRotation = Quaternion.Euler(0,0,0);
        }
    }
}
