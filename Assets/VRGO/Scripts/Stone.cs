
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
    Hited,
    Sended,
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

    [Header("VRCPickupを設定します")]
    [SerializeField] private VRCPickup pickup;

    [Header("マーカーを設定します")]
    [SerializeField] private GameObject marker;

    [Header("効果音を設定します")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sndPickup;
    [SerializeField] private AudioClip sndStrike;
    [SerializeField] private AudioClip sndTake;
    [SerializeField] private AudioClip sndReturn;
    [SerializeField] private AudioClip sndNormal;

    [System.NonSerialized] public int idx;
    [System.NonSerialized] public bool isBlack;

    private StoneState state = StoneState.Spawned;

    void Update()
    {
        if( Networking.IsOwner(gameObject) && state==StoneState.Hited && rigidbody.IsSleeping() ) {
            Debug.Log(state);
            if( gosys.isNormal ) {
                Vector2Int zahyo = gosys.GetZahyo(gameObject.transform.localPosition);
                if( Mathf.Abs(zahyo.x) <= 9 && Mathf.Abs(zahyo.y) <= 9 ) {
                    TeleportTo(gosys.GetNormalPosition(gameObject.transform.localPosition));
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayNormalSound));
                }
            }
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnSleepInOwner));
            state = StoneState.Sended;
        }
    }

    public void OnSleepInOwner()
    {
        var gosysOwner = Networking.GetOwner(gosys.gameObject);
        if( Networking.LocalPlayer == gosysOwner && gosys.status!=GoSystemStatus.Kento ) SendCustomEventDelayedSeconds(nameof(Record), 2.0f);
    }

    public void TeleportTo( Vector3 p )
    {
        gameObject.transform.localPosition = p;
        objectsync.FlagDiscontinuity();
    }

    public void Record()
    {
        gosys.WriteLog(this);
    }

    void OnDrop()
    {
        state = StoneState.Droped;
    }

    void OnPickup()
    {
        if ( state == StoneState.Spawned ) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayPickupSound));
        if ( state == StoneState.Sended ) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayTakeSound));
        state = StoneState.Pickup;
    }

    void OnCollisionStay(Collision col)
    {
        if ( !Networking.IsOwner(gameObject) ) return;

        // 碁盤に石が当たった音を再生します。
        if ( col.gameObject.layer==23 ) {
            if ( state==StoneState.Droped ) {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayStrikeSound));
                state = StoneState.Hited;
            }
        }

        if ( col.gameObject.layer==24 && state != StoneState.Pickup) {
            MarkerOff();
            if( state==StoneState.Droped ) {
                gosys.Return(this);
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayReturnSound));
            }
            state = StoneState.Spawned;
        }
    }

    public void onHandAreaEnter( Vector3 handPos )
    {
        pickup.pickupable = true;
    }

    public void onHandAreaExit()
    {
        pickup.pickupable = false;
    }

    public void PlayTakeSound(){ if(audioSource && sndTake) audioSource.PlayOneShot(sndTake); }
    public void PlayNormalSound(){ if(audioSource && sndNormal) audioSource.PlayOneShot(sndNormal); }
    public void PlayStrikeSound() { if(audioSource && sndStrike) audioSource.PlayOneShot(sndStrike); }
    public void PlayPickupSound() { if(audioSource && sndPickup) audioSource.PlayOneShot(sndPickup); }
    public void PlayReturnSound() { if(audioSource && sndReturn) audioSource.PlayOneShot(sndReturn); }

    public void MarkerOn() { gosys.AllMarkerOff(); marker.gameObject.SetActive(true); }
    public void MarkerOff() { marker.gameObject.SetActive(false); }
}
