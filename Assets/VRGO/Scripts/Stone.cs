
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Components;


public enum StoneState
{
    Spawned,
    Pickup,
    Droped,
    Stoped,
    Killing,
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class Stone : UdonSharpBehaviour
{
    [Header("GoManagerを設定します")]
    [SerializeField] private GoSystem gosys;

    [Header("Rigidbodyを設定します")]
    [SerializeField] private Rigidbody rigidbody;

    [Header("VRCObjectSyncを設定します")]
    [SerializeField] private VRCObjectSync objectsync;

    [Header("碁石の種類を設定します")]
    [SerializeField] public bool isBlack;

    [Header("碁石をとりやすくするためのコライダーを設定します")]
    [SerializeField] private Collider pickupCollider;

    [Header("効果音を設定します")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sndPickup;
    [SerializeField] private AudioClip sndStrike;
    [SerializeField] private AudioClip sndTake;
    [SerializeField] private AudioClip sndReturn;
    [SerializeField] private AudioClip sndNormal;

    [SerializeField] private Text info;

    [UdonSynced(UdonSyncMode.None)] private StoneState state = StoneState.Spawned;
    private bool isLoged;

    void Start()
    {
    }

    void Update()
    {
        pickupCollider.enabled = state == StoneState.Spawned;

        info.text = state.ToString()+"\n"+Networking.GetOwner(gameObject).displayName.Substring(0,6);

        // GoSystemの管理者PCがKilling状態の石を見つけたら値をリセットして石をReturnする。
        if( state==StoneState.Killing && Networking.LocalPlayer==Networking.GetOwner(gosys.gameObject) ) Return();

        if ( Networking.IsOwner(gameObject) && state==StoneState.Droped && rigidbody.IsSleeping() ) {
            state = StoneState.Stoped;
            SendCustomEventDelayedSeconds(nameof(SetOwnerToGosysOwner), 2.0f); // すぐオーナーを戻すとstateの同期が遅延するので遅らせてます

//            Networking.SetOwner(Networking.LocalPlayer, gameObject);
//            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayNormalSound));
        }

        // GoSystemの権利者から見た世界で落下中の石の権利が他人にあったら自分のものにする
//        if ( Networking.IsOwner(gosys.gameObject) && state==StoneState.Droped && !Networking.IsOwner(gameObject) ) {
  //          Networking.SetOwner(Networking.LocalPlayer, gameObject);
//        }
        
/*        if ( Networking.IsOwner(gameObject) !rigidbody.isKinematic && rigidbody.IsSleeping() ) {
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            Networking.SetOwner(Networking.GetOwner(gosys.gameObject), gameObject);
        }
            var gosysOwner = Networking.GetOwner(gosys.gameObject);
            if( Networking.LocalPlayer != gosysOwner ) Networking.SetOwner(gosysOwner, gameObject);
        
        */
    }

    public void SetOwnerToGosysOwner()
    {
        var gosysOwner = Networking.GetOwner(gosys.gameObject);
        if( Networking.LocalPlayer != gosysOwner ) Networking.SetOwner(gosysOwner, gameObject);
    }

    public void OnSpawn()
    {
//        state = StoneState.Spawned;
    }

    void OnDrop()
    {
//        objectsync.FlagDiscontinuity();
        rigidbody.isKinematic = false;
        state = StoneState.Droped;
    }

    void OnPickup()
    {
        // 石を取った音を全てのPCで再生します。
        string e = state==StoneState.Spawned ? nameof(PlayPickupSound) : nameof(PlayTakeSound);
        SendCustomNetworkEvent(NetworkEventTarget.All, e);

        // スポーンされた石を取ったら次の新しい石をスポーンします。
        if ( state == StoneState.Spawned ) {
            e = "Spawn" + (isBlack ? "Black" : "White");
            gosys.SendCustomNetworkEvent(NetworkEventTarget.Owner, e);
        }

//        rigidbody.constraints = RigidbodyConstraints.None;            
        state = StoneState.Pickup;
        isLoged = false;
    }

    void OnCollisionEnter(Collision col)
    {
        if ( !Networking.IsOwner(gameObject) ) return;
        if ( state != StoneState.Droped ) return;

        // 碁盤に石が当たった音を再生します。
        if ( col.gameObject.layer==23 && state==StoneState.Droped ) {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayStrikeSound));
        }

        // 碁笥に石を戻した音を再生して石をPoolに戻します。
        if ( col.gameObject.layer==24 && state==StoneState.Droped ) {
            state = StoneState.Killing;
            rigidbody.isKinematic = true;
            SendCustomEventDelayedSeconds(nameof(SetOwnerToGosysOwner), 2.0f);
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayReturnSound));
        }
    }

    public void Return()
    {
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Reset)); 
        SendCustomEventDelayedSeconds(nameof(Kill), 0.5f);
    }

    public void Reset()
    {
        rigidbody.isKinematic = true;
        state = StoneState.Spawned;
    }

    public void Kill() { gosys.killStone(this); }
    public void PlayTakeSound(){ if(audioSource && sndTake) audioSource.PlayOneShot(sndTake); }
    public void PlayNormalSound(){ if(audioSource && sndNormal) audioSource.PlayOneShot(sndNormal); }
    public void PlayStrikeSound() { if(audioSource && sndStrike) audioSource.PlayOneShot(sndStrike); }
    public void PlayPickupSound() { if(audioSource && sndPickup) audioSource.PlayOneShot(sndPickup); }
    public void PlayReturnSound() { if(audioSource && sndReturn) audioSource.PlayOneShot(sndReturn); }
}
