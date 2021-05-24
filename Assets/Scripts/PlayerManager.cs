using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public class PlayerManager : UdonSharpBehaviour
{
    private GameManager gameManager = null; // Not serialized because it would break as a prefab. Inserted by GameManager
    [SerializeField] private PlayerUI playerUI = null;
    private Prompts prompts = null; // Not serialized because it would break as a prefab. Inserted by GameManager
    [SerializeField] private GameObject stylus;
    
    [UdonSynced] private int ownerPlayerId = -1;
    private int ownerPlayerIdOld = -1;
    private int correctIndex = -1;
    
    public void SetPrompt(int index)
    {
        playerUI.SetPrompt(index, prompts);
    }

    private bool LocalIsOwner()
    {
        return ownerPlayerId > 0 && GetManagedPlayerByID().isLocal;
    }

    public override void OnDeserialization()
    {
        if (ownerPlayerId != ownerPlayerIdOld)
        {
            Debug.Log($"Owner player ID changed from {ownerPlayerIdOld} to {ownerPlayerId}");
            ownerPlayerIdOld = -1;
        }
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
    {
        return true;
    }

    private VRCPlayerApi GetManagedPlayerByID()
    {
        return VRCPlayerApi.GetPlayerById(ownerPlayerId);
    }

    public void RequestUpdateOwnerID()
    {
        Debug.Log("Asking master to become owner of this pen.");
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(UpdateOwnerID));
    }

    public void UpdateOwnerID()
    {
        if (!Networking.LocalPlayer.isMaster) return;
        Debug.Log("I'm the master. I'll try to add owner of this stylus to owner ID of the player manager.");
        gameManager.RemoveManagedPlayerId(Networking.GetOwner(stylus).playerId);
        ownerPlayerId = Networking.GetOwner(stylus).playerId;
        gameManager.RequestPlayerManagerSerialization();
        OnDeserialization();
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (GetManagedPlayerByID() == player) ownerPlayerId = -1;
    }

    public bool ResetManagedPlayedId(int playerId)
    {
        if (ownerPlayerId == playerId)
        {
            ownerPlayerId = -1;
            return true;
        }

        return false;
    }

    public void OnRoundChanged(int seed, int round)
    {
        playerUI.MakeAllPromptsNeutral();
        if (LocalIsOwner()) playerUI.SetPromptCorrect(GetCorrectIndex(seed, round));
    }

    private int GetCorrectIndex(int seed, int round)
    {
        Random.InitState(seed + round);
        return UnityEngine.Random.Range(0, 7);
    }

    public void SetPromptsAndGameManager(Prompts prompts1, GameManager gameManager1)
    {
        prompts = prompts1;
        gameManager = gameManager1;
    }
}
