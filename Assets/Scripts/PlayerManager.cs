﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class PlayerManager : UdonSharpBehaviour
{
    private GameManager gameManager = null; // Not serialized because it would break as a prefab. Inserted by GameManager
    [SerializeField] private PlayerUI playerUI = null;
    private Prompts prompts = null; // Not serialized because it would break as a prefab. Inserted by GameManager
    [SerializeField] private UdonBehaviour stylus;
    
    [UdonSynced] private int ownerPlayerId = -1;

    /// <summary>
    /// Index of prompt, index of player, index of prompt, index of player, ...
    /// </summary>
    [UdonSynced] private int[] votes = null;
    
    private int playerIndex = -1;
    private int ownerPlayerIdOld = -1;
    private int correctIndex = -1;

    public void SetButtonInfo(int pi)
    {
        playerIndex = pi;
        playerUI.SetButtonInfo(this);
    }
    
    public void SetPrompt(int index)
    {
        playerUI.SetPrompt(index, prompts);
    }

    public void SetColor(int colorIndex)
    {
        playerUI.SetColor(colorIndex);
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
        gameManager.RemoveManagedPlayerId(Networking.GetOwner(stylus.gameObject).playerId);
        ownerPlayerId = Networking.GetOwner(stylus.gameObject).playerId;
        gameManager.RequestPlayerManagerSerialization();
        OnDeserialization();
    }

    public int GetOwnerPlayerId()
    {
        return ownerPlayerId;
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
        
        if (round < 0) return;

        ResetVotes();
        ClearLines();
        correctIndex = GetCorrectIndex(seed, round);
        if (LocalIsOwner()) playerUI.SetPromptCorrect(correctIndex);
    }

    private void ClearLines()
    {
        stylus.SendCustomNetworkEvent(NetworkEventTarget.All, "ResetLines");
    }

    public int GetCorrectIndex(int seed, int round)
    {
        Random.InitState(seed + round);
        return UnityEngine.Random.Range(0, 6);
    }

    public void SetPromptsAndGameManager(Prompts prompts1, GameManager gameManager1)
    {
        prompts = prompts1;
        gameManager = gameManager1;
    }

    public void OnButtonPressed(int buttonIndex)
    {
        if (ownerPlayerId < 0)
        {
            OnVoteEmptyPlayer();
            return;
        }
        
        var myId = gameManager.GetMyPlayerManagerId();
        if (myId < 0)
        {
            OnVoteNotAllowed();
            return;
        }

        if (Networking.LocalPlayer.playerId == ownerPlayerId)
        {
            OnVoteOwnPrompt();
            return;
        }

        if (buttonIndex != correctIndex)
        {
            OnVoteSubmittedIncorret();
            return;
        }

        OnVoteSubmittedCorrect(myId);

        //playerUI.SetPromptState(buttonIndex, buttonIndex == correctIndex ? 2 : 1);
    }

    private string GetOwnerName()
    {
        return VRCPlayerApi.GetPlayerById(ownerPlayerId).displayName;
    }

    private void OnVoteSubmittedCorrect(int id)
    {
        Debug.Log($"CORRECT: {Networking.LocalPlayer.displayName} voted correctly for {GetOwnerName()}'s prompt {correctIndex}");
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(SubmitVoteCorrect) + "Player" + id);
        // TODO prevent further votes and stuff
    }

    private void OnVoteSubmittedIncorret()
    {
        Debug.Log($"WRONG: {Networking.LocalPlayer.displayName} voted incorrectly for {GetOwnerName()}'s prompt {correctIndex}");
        // TODO Lock votes
    }

    private void OnVoteOwnPrompt()
    {
        Debug.Log($"{GetOwnerName()} tried to vote for their own prompt");
    }

    private void OnVoteNotAllowed()
    {
        Debug.Log($"{Networking.LocalPlayer.displayName} tried to vote for {GetOwnerName()}, but that's not allowed right now.");
    }

    private void OnVoteEmptyPlayer(int id)
    {
        Debug.Log($"{Networking.LocalPlayer.displayName} tried to vote for index {playerIndex}, but there's no player there.");
    }

    private void ResetVotes()
    {
        votes = new int[8];
        for (var i = 0; i < votes.Length; i++)
        {
            votes[i] = -1;
        }
    }

    private void SubmitVoteCorrect(int pi)
    {
        for (var index = 0; index < votes.Length; index++)
        {
            if (pi == votes[index])
            {
                Debug.LogWarning($"Player {pi} attempted to vote twice");
                return;
            }

            if (votes[index] == -1)
            {
                votes[index] = pi;
                Debug.Log($"Correct vote received by player {pi}");
                return;
            }
        }
    }
    
    public void SubmitVoteCorrectPlayer0()
    {
        SubmitVoteCorrect(0);
    } 
    
    public void SubmitVoteCorrectPlayer1()
    {
        SubmitVoteCorrect(1);
    } 
    
    public void SubmitVoteCorrectPlayer2()
    {
        SubmitVoteCorrect(2);
    } 
    
    public void SubmitVoteCorrectPlayer3()
    {
        SubmitVoteCorrect(3);
    } 
    
    public void SubmitVoteCorrectPlayer4()
    {
        SubmitVoteCorrect(4);
    } 
    
    public void SubmitVoteCorrectPlayer5()
    {
        SubmitVoteCorrect(5);
    } 
    
    public void SubmitVoteCorrectPlayer6()
    {
        SubmitVoteCorrect(6);
    } 
    
    public void SubmitVoteCorrectPlayer7()
    {
        SubmitVoteCorrect(7);
    } 
}
