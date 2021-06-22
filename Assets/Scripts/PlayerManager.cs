using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public class PlayerManager : UdonSharpBehaviour
{
    private GameManager gameManager = null; // Not serialized because it would break as a prefab. Inserted by GameManager

    [SerializeField] private PlayerUI playerUI = null;
    private Prompts prompts = null; // Not serialized because it would break as a prefab. Inserted by GameManager
    [SerializeField] private StylusSharp stylus;
    [SerializeField] private MeshRenderer[] meshesToRecolors = null; 
    
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
    private int scoreOld = 0;

    private int playerIndex = -1;
    private int ownerPlayerIdOld = -1;
    private int correctIndex = -1;
    private ScoreScript scoreScript;

    private bool localHasVoted = false;
    [SerializeField] private Material[] materialsColor = null;
    
    private void Start()
    {
        ResetVotes();
        playerUI.SetPromptsVisible(true);
    }

    private void FixedUpdate()
    {
        // TODO Does this fix the late joiner sync?
        playerUI.SetPromptsVisible(ownerPlayerId > -1 && isPlaying);
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
        if (gameManager != null)
        {
            var playerCount = gameManager.GetPlayerCount();
            var pointsArray = scoreScript.GetGuessPointsArray(playerCount);
            if (pointsArray == null)
            {
                Debug.LogError("PointsArray was null! Aborting votes update");
                return;
            }
            
            var networkIds = gameManager.GetPlayerNetworkIds(votes);
            
            playerUI.SetVoteResults(votes, true, pointsArray, networkIds); // TODO isRevealed is always true
        }
        else
        {
            Debug.LogWarning("PlayerManager was null when updating votes!!");
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
    }
    
    public void SetPrompt(int index, bool quickSpin)
    {
        playerUI.SetPrompt(index, prompts, quickSpin);
    }

    public void SetColor(int colorIndex)
    {
        var mat = materialsColor[colorIndex];
        playerUI.SetColor(mat);
        stylus.SetColor(colorIndex);
        foreach (var m in meshesToRecolors)
        {
            m.material = mat;
        }
    }
    
    public bool LocalIsOwner()
    {
        if (ownerPlayerId <= 0) return false;
        var managedPlayer = VRCPlayerApi.GetPlayerById(ownerPlayerId);
        if (Utilities.IsValid(managedPlayer)) return managedPlayer.isLocal;
        
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

    public override void OnDeserialization()
    {
        Debug.Log("OnDeserialization for " + playerIndex);
        if (ownerPlayerId != ownerPlayerIdOld)
        {
            Debug.Log($"Owner player ID of pen {playerIndex} changed from {ownerPlayerIdOld} to {ownerPlayerId}");
            ownerPlayerIdOld = ownerPlayerId;
            UpdateInstructions();
            var local = Networking.LocalPlayer;
            if (local == null) return;
            var localId = Networking.LocalPlayer.playerId;
            if (ownerPlayerId < 0 || localId == ownerPlayerId) stylus.SetColliderEnabled(true);
            else stylus.SetColliderEnabled(false);
 
        }

        if (score != scoreOld)
        {
            Debug.Log("Score has changed: Player" + playerIndex);
            scoreOld = score;
            playerUI.SetScore(score);
        }

        UpdateVotesLocal(); // TODO Remove this. Probably not needed due to the networked event which calls this
        Debug.Log("OnDeserialization complete");
    }

    private void UpdateInstructions()
    {
        Debug.Log($"Updating player {playerIndex} instructions");
        if (gameManager == null)
        {
            Debug.LogWarning("gameManager was used before it was properly set!");
            return;
        }

        var round = gameManager.GetRound();
        var roundOver = gameManager.IsRoundOver();
        playerUI.UpdateInstructions(round, GetOwnerName(), LocalIsOwner(), LocalHasVotedForThis(), roundOver, isPlaying);
        Debug.Log("Instruction update complete");
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
    {
        return true;
    }

    /// <summary>
    /// Called from the graph when ownership changed or when this is picked up.
    /// Either way, the master should correctly update the owner ID when this method is called.
    /// </summary>
    public void UpdateOwnerID()
    {
        if (!Networking.LocalPlayer.isMaster) return;
        Debug.Log("I'm the master. I'll try to add owner of this stylus to owner ID of the player manager.");
        var tempId = Networking.GetOwner(stylus.gameObject).playerId;
        if (tempId == ownerPlayerId) return; 
        gameManager.RemoveManagedPlayerId(tempId);
        ownerPlayerId = tempId;
        gameManager.RequestPlayerManagerSerialization();
        OnDeserialization();
    }

    public int GetOwnerPlayerId()
    {
        return ownerPlayerId;
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

        if (round < 0)
        {
            UpdateInstructions();
            return;
        }
        if (Networking.IsMaster) AskOwnerClearLines();

        if (!Utilities.IsValid(VRCPlayerApi.GetPlayerById(ownerPlayerId))) ownerPlayerId = -1;
        if (ownerPlayerId < 0)
        {
            isPlaying = false;
            playerUI.SetPromptsVisible(false);
            UpdateInstructions();
            return;
        }
        
        playerUI.SetPromptsVisible(true);
        isPlaying = true;
        
        ResetVotes();
        correctIndex = GetCorrectIndex(seed, round);
        if (LocalIsOwner()) playerUI.SetPromptCorrect(correctIndex);
        
        UpdateInstructions();
    }
    
    private void AskOwnerClearLines()
    {
        stylus.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(StylusSharp.Erase));
    }

    public int GetCorrectIndex(int seed, int round)
    {
        Debug.Log($"Generating correct prompt from seed {seed} round {round} index {playerIndex}");
        var newSeed = seed + round + playerIndex;
        UnityEngine.Random.InitState(newSeed);
        var index = UnityEngine.Random.Range(0, 6);
        Debug.Log($"Generated correct index {index} from seed {newSeed}");
        return index;
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
        if (gameManager.GetRound() < 0) return;
        if (gameManager.IsRoundOver()) return;
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
        return Utilities.IsValid(player) ? player.displayName : "!ERROR, TELL FAX!";
    }

    private void OnVoteSubmittedCorrect(int id, int index)
    {
        Debug.Log($"CORRECT: {Networking.LocalPlayer.displayName} voted correctly for {GetOwnerName()}'s prompt {correctIndex}");
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(SubmitVote) + "CorrectPlayer" + id);
        OnValidVoteSubmitted(index, true);
    }

    private void OnVoteSubmittedIncorrect(int id, int index)
    {
        Debug.Log($"WRONG: {Networking.LocalPlayer.displayName} voted incorrectly for {GetOwnerName()}'s prompt {correctIndex}");
        var eventName = "SubmitVoteWrongPlayer" + id;
        SendCustomNetworkEvent(NetworkEventTarget.Owner, eventName);
        OnValidVoteSubmitted(index, false);
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

    private void OnValidVoteSubmitted(int index, bool isCorrect)
    {
        localHasVoted = true;
        playerUI.SetPromptState(index, isCorrect ? 2 : 1);
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
    /// How many points would a player get for the vote they've submitted to this manager?
    /// </summary>
    /// <param name="p">Index of the player who votes</param>
    /// <param name="playerCount">Player count in this game</param>
    /// <returns>How many points they got</returns>
    public int GetPointsVoteCorrect(int playerCount, int p)
    {
        for (var index = 0; index < votes.Length; index++)
        {
            var vote = votes[index];
            if (vote == p)
            {
                var points = scoreScript.GetGuessPoints(playerCount, index);
                return points;
            }
        }

        return 0;
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
        Debug.Log($"Adding {points} points to player {playerIndex}");
        score += points;
    }

    public void OnRoundOver()
    {
        for (var i = 0; i < 6; i++)
        {
            if (i == correctIndex) playerUI.SetPromptCorrect(i);
            else playerUI.SetPromptWrong(i);
        }

        UpdateInstructions();
    }

    public int GetPointsDrawingHasBeenGuessed(int playerCount)
    {
        Debug.Log(nameof(GetPointsDrawingHasBeenGuessed));
        var points = 0;
        var pointsArray = scoreScript.GetGuessPointsArray(playerCount);
        var scoreIndex = 0;
        foreach (var voteIndex in votes)
        {
            var isSubmitted = voteIndex > -1;
            if (!isSubmitted) continue;
            var isCorrect = voteIndex < 10;
            if (!isCorrect) continue;
            points += pointsArray[scoreIndex];
            scoreIndex++;
        }

        return points;
    }

    public void Reset()
    {
        ResetVotes();
        playerUI.MakeAllPromptsNeutral();
        playerUI.ResetAnimatorState();
        score = 0;
        RequestSerialization();
        OnDeserialization();
    }
}