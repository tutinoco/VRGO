using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class SilentSphere : UdonSharpBehaviour
{
    [Header("コライダーが入ったオブジェクトを設定")]
    public GameObject collider;
    public float onScale;
    public float offScale;

    private int[] inRoomPlayers = new int[80];
    private bool inRoom = false;

    public void enableSilent()
    {
        collider.transform.localScale = new Vector3(onScale,onScale,onScale);
    }

    public void disableSilent()
    {
        collider.transform.localScale = new Vector3(offScale,offScale,offScale);
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player == null) return;
        if (player == Networking.LocalPlayer) { inRoom = true; }
        for (int i = 0; i < inRoomPlayers.Length; i++)
        {
            if (inRoomPlayers[i] == 0) { inRoomPlayers[i] = player.playerId; break; }
            else { }
        }
        SetRoomVoice();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (player == null) return;
        if (player == Networking.LocalPlayer) { inRoom = false; }
        for (int i = 0; i < inRoomPlayers.Length; i++)
        {
            if (inRoomPlayers[i] == player.playerId) { inRoomPlayers[i] = 0; }
            else { }
        }
        SetRoomVoice();
    }

    private void SetRoomVoice()
    {
        VRCPlayerApi[] players = new VRCPlayerApi[80];
        VRCPlayerApi.GetPlayers(players);
        for (int j = 0; j < players.Length; j++)
        {
            if (players[j] != null)
            {
                bool flag = false;
                for (int i = 0; i < inRoomPlayers.Length; i++)
                {
                    if (inRoomPlayers[i] == players[j].playerId)
                    {

                        if (inRoom) { SetVoiceDefault(players[j].playerId); } else { SetVoiceZero(players[j].playerId); }
                        flag = true;
                    }
                    else { }
                }
                if (flag == false) { SetVoiceDefault(players[j].playerId); }
            }
        }
    }

    private void SetVoiceZero(int id)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(id);
        if (player == null) return;
        player.SetVoiceGain(0);
        player.SetVoiceDistanceNear(0);
        player.SetVoiceDistanceFar(0);
    }

    private void SetVoiceDefault(int id)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(id);
        if (player == null) return;
        player.SetVoiceGain(15);
        player.SetVoiceDistanceNear(0);
        player.SetVoiceDistanceFar(25);
    }
}
