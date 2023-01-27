
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Zabuton : UdonSharpBehaviour
{
    [Header("目に見えるモデルを設定します")]
    [SerializeField] private GameObject model;

    [Header("コライダーを設定します")]
    [SerializeField] private Collider collider;

    void Update()
    {
        Vector3 p = gameObject.transform.localPosition;
        model.gameObject.transform.localPosition = new Vector3(0, Mathf.Max(Mathf.Min(p.y,0.35f), -0.1f), 0);
        model.gameObject.transform.localRotation = Quaternion.Euler(0,0,0);
    }

    void OnDrop()
    {
        Vector3 p = gameObject.transform.localPosition;
        gameObject.transform.localPosition = new Vector3(0, Mathf.Max(Mathf.Min(p.y,0.35f), -0.1f), 0);
        gameObject.transform.localRotation = Quaternion.Euler(0,0,0);
        collider.transform.localPosition = gameObject.transform.localPosition;
    }
}
