using System;
using System.Linq;
using UdonSharp;
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
    /// Votes by players who guessed correctly. Ordered by who guessed first.
    /// 0 = player 0 voted correctly
    /// 1 = player 1 voted correctly, and so on.
    ///
    /// Also contains wrong votes! // TODO
    /// 10 = player 0 voted incorrectly
    /// 11 = player 1 voted incorrectly, and so on.
    /// </summary>
    [UdonSynced] private int[] votes = null;

    [UdonSynced] private bool isPlaying = false;

    [UdonSynced] private int score = 0;

    private int playerIndex = -1;
    private int ownerPlayerIdOld = -1;
    private int correctIndex = -1;
    private ScoreScript scoreScript;

    private bool localHasVoted = false;

    private void Start()
    {
        ResetVotes();
    }

    private void SetIsPlaying(bool b)
    {
        isPlaying = b;
    }

    public bool GetIsPlaying()
    {
        return isPlaying;
    }

    private void UpdateVotesEveryone()
    {
        Debug.Log("Telling all players to update their vote displays...");
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateVotesLocal));
    }
    
    public void UpdateVotesLocal()
    {
        Debug.Log("Master told us to update out vote displays!");
        if (gameManager != null)
        {
            var playerCount = gameManager.GetPlayerCount();
            var pointsArray = scoreScript.GetGuessPointsArray(playerCount);
            int[] networkIds = gameManager.GetPlayerNetworkIds(votes);
            
            playerUI.SetVoteResults(votes, true, pointsArray, networkIds); // TODO isRevealed is always true
        }
        else
        {
            playerUI.HideVoteResults();
        }
        
    }

    public void InsertDummyPlayer()
    {
        ownerPlayerId = 123456;
        gameManager.RequestPlayerManagerSerialization();
        OnDeserialization();
    }

    public void SetButtonInfo(int pi)
    {
        playerIndex = pi;
        playerUI.SetButtonInfo(this);
        UpdateInstructions();
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
        if (ownerPlayerId <= 0) return false;
        var managedPlayer = VRCPlayerApi.GetPlayerById(ownerPlayerId);
        if (managedPlayer == null)
        {
            Debug.LogWarning("Managed player was not cleared before LocalIsOwner was called. Odd!");
            if (ownerPlayerId == 123456)
            {
                Debug.Log("Oh, it's a dummy player! False alarm, carry on.");
            }
            else
            {
                ownerPlayerId = -1;
            }

            return false;

        }
        return managedPlayer.isLocal;
    }

    public override void OnDeserialization()
    {
        if (ownerPlayerId != ownerPlayerIdOld)
        {
            Debug.Log($"Owner player ID of pen {playerIndex} changed from {ownerPlayerIdOld} to {ownerPlayerId}");
            ownerPlayerIdOld = ownerPlayerId;
            UpdateInstructions();
        }

        UpdateVotesLocal(); // TODO Remove this. Probably not needed due to the networked event which calls this
    }

    private void UpdateInstructions()
    {
        playerUI.UpdateInstructions(gameManager.GetRound(), GetOwnerName(), LocalIsOwner(), LocalHasVotedForThis());
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
    {
        return true;
    }

    /*
     No longer called from the graph
    /// <summary>
    /// Called from the stylus Udon graph to update ownership.
    /// </summary>
    public void RequestUpdateOwnerID()
    {
        Debug.Log("Asking master to become owner of this pen.");
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(UpdateOwnerID));
    }
    */

    /// <summary>
    /// Called from the graph when ownership changed or when this is picked up.
    /// Either way, the master should correctly update the owner ID when this method is called.
    /// </summary>
    public void UpdateOwnerID()
    {
        if (!Networking.LocalPlayer.isMaster) return;
        Debug.Log("I'm the master. I'll try to add owner of this stylus to owner ID of the player manager.");
        var tempId = Networking.GetOwner(stylus.gameObject).playerId;
        gameManager.RemoveManagedPlayerId(tempId);
        ownerPlayerId = tempId;
        gameManager.RequestPlayerManagerSerialization();
        OnDeserialization();
    }

    public int GetOwnerPlayerId()
    {
        return ownerPlayerId;
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (ownerPlayerId == player.playerId) ownerPlayerId = -1;
    }

    public bool ResetManagedPlayedId(int playerId)
    {
        if (ownerPlayerId == playerId)
        {
            ownerPlayerId = -1;
            OnDeserialization();
            return true;
        }

        return false;
    }

    public void OnRoundChanged(int seed, int round)
    {
        playerUI.MakeAllPromptsNeutral();
        localHasVoted = false;
        UpdateInstructions();
        
        if (round < 0) return;
        ClearLines();
        if (ownerPlayerId < 0)
        {
            playerUI.ClearText();
            isPlaying = false;
            return;
        }

        isPlaying = true;
        
        ResetVotes();
        correctIndex = GetCorrectIndex(seed, round);
        if (LocalIsOwner()) playerUI.SetPromptCorrect(correctIndex);
    }

    private void ClearLines()
    {
        Debug.Log("Asking owner to clear drawn stylus lines");
        stylus.SendCustomNetworkEvent(NetworkEventTarget.Owner, "Erase");
    }

    public int GetCorrectIndex(int seed, int round)
    {
        UnityEngine.Random.InitState(seed + round);
        return UnityEngine.Random.Range(0, 6);
    }

    public void SetPromptsAndGameManager(Prompts prompts1, GameManager gameManager1, ScoreScript s)
    {
        prompts = prompts1;
        gameManager = gameManager1;
        scoreScript = s;
        UpdateInstructions();
    }

    public void OnButtonPressed(int buttonIndex)
    {
        if (LocalHasVotedForThis()) return;
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
            OnVoteSubmittedIncorrect(myId, buttonIndex);
            return;
        }

        OnVoteSubmittedCorrect(myId, buttonIndex);

        //playerUI.SetPromptState(buttonIndex, buttonIndex == correctIndex ? 2 : 1);
    }

    private bool LocalHasVotedForThis()
    {
        return localHasVoted;
        /*
        Debug.Log("Checking whether local voted for this");
        if (gameManager == null) return false;
        var myId = gameManager.GetMyPlayerManagerId();
        if (myId < 0) return true;
        foreach (var vote in votes)
        {
            Debug.Log("Vote " + vote);
            if (vote == myId || vote - 10 == myId) return true;
        }

        Debug.Log("Player was not in vote list.");
        return false;*/
    }

    public string GetOwnerName()
    {
        if (ownerPlayerId < 0) return null;
        var player = VRCPlayerApi.GetPlayerById(ownerPlayerId);
        return player != null ? player.displayName : "!ERROR, TELL FAX!";
    }

    private void OnVoteSubmittedCorrect(int id, int index)
    {
        Debug.Log($"CORRECT: {Networking.LocalPlayer.displayName} voted correctly for {GetOwnerName()}'s prompt {correctIndex}");
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(SubmitVote) + "CorrectPlayer" + id);
        OnValidVoteSubmitted(index);
    }

    private void OnVoteSubmittedIncorrect(int id, int index)
    {
        Debug.Log($"WRONG: {Networking.LocalPlayer.displayName} voted incorrectly for {GetOwnerName()}'s prompt {correctIndex}");
        var eventName = "SubmitVoteWrongPlayer" + id;
        SendCustomNetworkEvent(NetworkEventTarget.Owner, eventName);
        OnValidVoteSubmitted(index);
    }

    private void OnVoteOwnPrompt()
    {
        Debug.Log($"{GetOwnerName()} tried to vote for their own prompt");
    }

    private void OnVoteNotAllowed()
    {
        Debug.Log($"{Networking.LocalPlayer.displayName} tried to vote for {GetOwnerName()}, but that's not allowed right now.");
    }

    private void OnVoteEmptyPlayer()
    {
        Debug.Log($"{Networking.LocalPlayer.displayName} tried to vote for index {playerIndex}, but there's no player there.");
    }

    private void OnValidVoteSubmitted(int index)
    {
        localHasVoted = true;
        playerUI.SetPromptState(index, -1);
        UpdateInstructions();
    }

    private void ResetVotes()
    {
        votes = new int[8];
        for (var i = 0; i < votes.Length; i++)
        {

            votes[i] = -1;
        }

        RequestSerialization();
        OnDeserialization();
    }

    private void SubmitVote(int pi, bool correct)
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
                votes[index] = pi + (correct ? 0 : 10);
                Debug.Log((correct ? "Correct" : "Wrong") + $" vote {votes[index]} received by player {pi}");
                
                RequestSerialization();
                OnDeserialization();
                gameManager.TryBonusPointPlacement(pi);
                UpdateVotesEveryone();
                return;
            }
        }
        
        Debug.LogWarning("Um ran out of space in the votes array. What");
    }

    /// <summary>
    /// Whether the given player index has voted for the player represented by this player manager.
    /// </summary>
    /// <param name="p">Index of the player to check for</param>
    /// <returns>Whether that player has voted for this player manager</returns>
    public bool HasBeenVotedForBy(int p)
    {
        Debug.Log($"Checking whether {p} has been voted for {playerIndex}");
        foreach (var vote in votes)
        {
            if (vote == p || vote - 10 == p) return true;
        }

        return false;
    }
    
    public void SubmitVoteCorrectPlayer0()
    {
        SubmitVote(0, true);
    } 
    
    public void SubmitVoteCorrectPlayer1()
    {
        SubmitVote(1, true);
    } 
    
    public void SubmitVoteCorrectPlayer2()
    {
        SubmitVote(2, true);
    } 
    
    public void SubmitVoteCorrectPlayer3()
    {
        SubmitVote(3, true);
    } 
    
    public void SubmitVoteCorrectPlayer4()
    {
        SubmitVote(4, true);
    } 
    
    public void SubmitVoteCorrectPlayer5()
    {
        SubmitVote(5, true);
    } 
    
    public void SubmitVoteCorrectPlayer6()
    {
        SubmitVote(6, true);
    } 
    
    public void SubmitVoteCorrectPlayer7()
    {
        SubmitVote(7, true);
    }
    
    public void SubmitVoteWrongPlayer0()
    {
        SubmitVote(0, false);
    } 
    
    public void SubmitVoteWrongPlayer1()
    {
        SubmitVote(1, false);
    } 
    
    public void SubmitVoteWrongPlayer2()
    {
        SubmitVote(2, false);
    } 
    
    public void SubmitVoteWrongPlayer3()
    {
        SubmitVote(3, false);
    } 
    
    public void SubmitVoteWrongPlayer4()
    {
        SubmitVote(4, false);
    } 
    
    public void SubmitVoteWrongPlayer5()
    {
        SubmitVote(5, false);
    } 
    
    public void SubmitVoteWrongPlayer6()
    {
        SubmitVote(6, false);
    } 
    
    public void SubmitVoteWrongPlayer7()
    {
        SubmitVote(7, false);
    }


    public void AddPoints(int points)
    {
        score += points;
    }
}
