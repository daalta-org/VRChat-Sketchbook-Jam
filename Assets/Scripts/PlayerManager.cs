using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PlayerManager : UdonSharpBehaviour
{
    [SerializeField] private GameManager gameManager = null;
    [SerializeField] private PlayerUI playerUI = null;
    [SerializeField] private Prompts prompts = null;
    [UdonSynced] private int managedPlayerID = -1;
    private int managedPlayerIDold = -1;
    private int prompt = -1;
    private int correctIndex = -1;

    public void SetPrompt(int index)
    {
        prompt = index;
        playerUI.SetPrompt(index, prompts);
    }

    public void SetCorrectIndex(int index)
    {
        correctIndex = index;
        playerUI.SetCorrectPrompt(index, managedPlayerID > 0 && GetManagedPlayerByID().isLocal);
    }

    public override void OnDeserialization()
    {
        if (managedPlayerID != managedPlayerIDold)
        {
            managedPlayerIDold = -1;
        }
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
    {
        return true;
    }

    private VRCPlayerApi GetManagedPlayerByID()
    {
        return VRCPlayerApi.GetPlayerById(managedPlayerID);
    }

    public void BecomePlayer()
    {
        gameManager.ResetAllPlayerManagedPlayedIds(Networking.LocalPlayer.playerId);
        managedPlayerID = Networking.LocalPlayer.playerId;
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (GetManagedPlayerByID() == player) managedPlayerID = -1;
    }

    public void ResetManagedPlayedId(int playerId)
    {
        if (managedPlayerID == playerId)
        {
            managedPlayerID = -1;
            playerUI.Reset();
        }
        OnDeserialization();
    }
}
