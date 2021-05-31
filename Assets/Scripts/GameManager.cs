using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public class GameManager : UdonSharpBehaviour
{
    [SerializeField] private GameUI gameUI = null;
    [SerializeField] private PlayerManager[] playerManagers = null;
    [SerializeField] private Prompts prompts = null;
    [SerializeField] private int numRounds = 5;
    [SerializeField] private ScoreScript scoreScript;

    [UdonSynced] private int seed = -1;
    private int seedOld = -1;
    [UdonSynced] private int round = -1;
    private int roundOld = -1;

    public int[] promptSequence = null;

    [UdonSynced] private int playerCount = -1;

    [UdonSynced] private int[] bonusPointPlacement;

    [UdonSynced] private bool isRoundOver = false;
    private bool isRoundOverOld = false;
    
    private void Start()
    {
        Debug.Log("Executing start event on game manager");
        SetPlayerColors();

        for (var index = 0; index < playerManagers.Length; index++)
        {
            var p = playerManagers[index];
            p.SetPromptsAndGameManager(prompts, this, scoreScript);
            p.SetButtonInfo(index);
        }

        if (!Networking.IsMaster) return;
        ResetBonusPointPlacement();
    }

    private void SetPlayerColors()
    {
        for (var index = 0; index < playerManagers.Length; index++)
        {
            playerManagers[index].SetColor(index);
        }
    }

    public void RequestStartGame()
    {
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(StartGame));
    }

    public override void OnDeserialization()
    {
        DealWithDeserialization();
    }

    public void StartGame()
    {
        if (!Networking.LocalPlayer.isMaster)
        {
            Debug.Log("Non-Master tried to call start game event. That's kinda sus");
            return;
        }

        seed = Time.frameCount;

        var numOfPlayers = CountPlayers(false);

        if (numOfPlayers < 3)
        {
            Debug.Log("Player count is below 3.");
            return;
        }
        seed = UnityEngine.Random.Range(-int.MaxValue, int.MaxValue);
        round = 0;
        foreach (var p in playerManagers)
        {
            p.Reset();
        }
        Debug.Log("Requesting Deserialization...");
        RequestSerialization();
        DealWithDeserialization();
    }

    public int GetPlayerCount()
    {
        return playerCount;
    }

    public void DealWithDeserialization()
    {
        Debug.Log("OnDeserialization has been called");

        if (seed != seedOld)
        {
            Debug.Log("Seed has changed: " + seed);
            seedOld = seed;
            OnSeedChanged();
        }

        if (round != roundOld)
        {
            Debug.Log("Round has changed: " + round);
            roundOld = round;
            OnRoundChanged();
        }

        if (isRoundOver != isRoundOverOld)
        {
            isRoundOverOld = isRoundOver;
            if (isRoundOver && round >= 0) OnRoundOver();
        }
        
        UpdateBonusPointUI(); // TODO probably happens too often
    }

    private void OnRoundOver()
    {
        Debug.Log("Round is over!!");
        gameUI.OnRoundOver();
        
        for (var index = 0; index < playerManagers.Length; index++)
        {
            Debug.Log("OnRoundOver for player " + index);

            playerManagers[index].OnRoundOver(); // reveals which prompts were correct
            if (!Networking.IsMaster) continue;
            var pointsToAdd = 0;
            pointsToAdd += GetPointsCorrectOnOtherPlayers(index);
            pointsToAdd += playerManagers[index].GetPointsDrawingHasBeenGuessed(GetPlayerCount());
            playerManagers[index].AddPoints(pointsToAdd);
        }
        
        if (!Networking.IsMaster) return;

        ApplyBonusPoints();
        RequestPlayerManagerSerialization();
        RequestSerialization();
        DealWithDeserialization();
    }

    public void RequestNextRound()
    {
        if (!isRoundOver || round >= 4) return;
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(NextRound));
        Debug.Log("Requesting that the master progresses to the next round...");
    }
    
    public void NextRound()
    {
        var playerCountTemp = CountPlayers(false);
        if (!isRoundOver || round >= 4 || playerCountTemp < 3) return;
        Debug.Log("Master: Increasing round counter and sending it to clients.");
        round++;
        isRoundOver = false;
        playerCount = playerCountTemp;
        RequestSerialization();
        DealWithDeserialization();
    }

    private void OnSeedChanged()
    {
        promptSequence = prompts.GetPrompts(seed, 4 * numRounds);
        if (promptSequence.Length != numRounds * 4)
        {
            Debug.LogWarning("prompt sequence has unexpected length: " + promptSequence.Length);
        }
    }

    private void OnRoundChanged()
    {
        SetPromptsForPlayersThisRound();
        ResetBonusPointPlacement();
        foreach (var playerManager in playerManagers)
        {
            playerManager.OnRoundChanged(seed, round);
        }

        playerCount = CountPlayers(true);

        gameUI.OnRoundChanged(round);
    }

    private int CountPlayers(bool checkIsPlaying)
    {
        var count = 0;
        foreach (var p in playerManagers)
        {
            if ((!checkIsPlaying || p.GetIsPlaying()) && Utilities.IsValid(VRCPlayerApi.GetPlayerById(p.GetOwnerPlayerId()))) count++;
        }

        return count;
    }
    
    private void SetPromptsForPlayersThisRound()
    {
        Debug.Log("Settings prompts for players for round " + round);
        UnityEngine.Random.InitState(seed);
        var promptsThisRound = prompts.GetPromptSequenceForRound(promptSequence, round);
        for (var i = 0; i < playerManagers.Length; i++)
        {
            playerManagers[i].SetPrompt(promptsThisRound[i]);
        }
    }

    public void RemoveManagedPlayerId(int playerId)
    {
        Debug.Log("Trying to remove playerId " + playerId + " from all pens.") ;
        if (playerId < 0)
        {
            Debug.LogWarning($"Someone asked to remove ID {playerId} from all pens. That's an invalid ID though!");
            return;
        }
        
        for (var index = 0; index < playerManagers.Length; index++)
        {
            var p = playerManagers[index];
            if (p.ResetManagedPlayedId(playerId))
            {
                Debug.Log($"Pen {index} was removed from player {playerId}");
                return;
            }
        }

        Debug.Log($"Player {playerId} did not previously own any pens.");
    }

    public void RequestPlayerManagerSerialization()
    {
        if (!Networking.LocalPlayer.isMaster) return;
        foreach (var p in playerManagers)
        {
            p.RequestSerialization();
            p.OnDeserialization();
        }
    }

    public int GetMyPlayerManagerId()
    {
        for (var index = 0; index < playerManagers.Length; index++)
        {
            var p = playerManagers[index];
            if (p.GetOwnerPlayerId() == Networking.LocalPlayer.playerId)
            {
                return index;
            }
        }

        return -1;
    }

    public int GetRound()
    {
        return round;
    }

    public int[] GetPlayerNetworkIds(int[] playerIndices)
    {
        var result = new int[playerIndices.Length];
        for (var i = 0; i < playerIndices.Length; i++)
        {
            var index = playerIndices[i];
            if (index == -1) return result;
            if (index >= 10) index -= 10; // False votes are 10 higher than normal. Remove that. Modulo would also work
            var id = playerManagers[index].GetOwnerPlayerId();
            result[i] = id;
        }

        return result;
    }

    /// <summary>
    /// Try to give the player bonus points, unless they're not done yet or are already placed.
    /// </summary>
    /// <param name="playerIndex">Index of the player top attempt the bonus point placement for.</param>
    public void TryBonusPointPlacement(int playerIndex)
    {
        if (HasPlacement(playerIndex))
        {
            Debug.LogWarning("Player already has a placement! This function should not have been called.");
            return;
        }

        if (!HasVotedForEveryone(playerIndex))
        {
            Debug.Log("Player has not yet voted for everyone, so they can't get bonus points yet.");
            return;
        }

        gameUI.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(gameUI.MusicDoStageTwo));

        for (var i = 0; i < bonusPointPlacement.Length; i++)
        {
            if (bonusPointPlacement[i] < 0)
            {
                bonusPointPlacement[i] = playerIndex;
                Debug.Log($"Player {playerIndex} will get bonus points for finishing!");

                UpdateIsRoundOver(); //isRoundOver = i + 1 >= CountPlayers() - 1; // 2 players (1 + 1) >= 2 players (3 - 1)
                
                gameUI.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(gameUI.MusicDoStageThree));
                
                RequestSerialization();
                DealWithDeserialization();
                return;
            }
        }
        
        Debug.LogWarning("Ran out of space for bonus point placement!");
    }

    private void UpdateIsRoundOver()
    {
        isRoundOver = GetNumPlacements() >= CountPlayers(true) - 1;// 2 players (1 + 1) >= 2 players (3 - 1)
        if (isRoundOver && isRoundOver != isRoundOverOld)
        {
            RequestSerialization();
            DealWithDeserialization();
        }
    }

    private void UpdateBonusPointUI()
    {
        var arrayLen = GetNumPlacements();
        
        var points = new int[arrayLen];
        var names = new string[arrayLen];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = scoreScript.GetBonusPoints(GetPlayerCount(), i);
            names[i] = playerManagers[bonusPointPlacement[i]].GetOwnerName();
        }
        gameUI.SetBonusPoints(points, names);
    }

    private bool HasPlacement(int playerIndex)
    {
        foreach (var p in bonusPointPlacement)
        {
            if (p == playerIndex) return true;
        }

        return false;
    }

    /// <summary>
    /// Gives each player the bonus points depending on their placement
    /// </summary>
    private void ApplyBonusPoints()
    {
        for (var index = 0; index < bonusPointPlacement.Length; index++)
        {
            var playerIndex = bonusPointPlacement[index];
            if (playerIndex < 0) return;
            var points = scoreScript.GetBonusPoints(GetPlayerCount(), index);
            Debug.Log($"Player {playerIndex} gets {points} bonus points for their No. {index+1} placement");
            playerManagers[playerIndex].AddPoints(points);
        }
    }

    /// <summary>
    /// Get the points received from voting correctly on other players' prompts
    /// </summary>
    private int GetPointsCorrectOnOtherPlayers(int playerIndex)
    {
        var total = 0;
        foreach (var p in playerManagers)
        {
            total += p.GetPointsVoteCorrect(GetPlayerCount(), playerIndex);
        }

        return total;
    }

    /// <summary>
    /// How many points did this player receive because their drawing was guessed correctly?
    /// </summary>
    /// <param name="playerIndex">Index of player manager to check</param>
    /// <returns>How many points they receive</returns>
    private int GetPointsDrawingHasBeenGuessed(int playerIndex)
    {
        var total = 0;
        foreach (var p in playerManagers)
        {
            total += p.GetPointsDrawingHasBeenGuessed(GetPlayerCount());
        }

        return total;
    }

    private bool HasVotedForEveryone(int playerIndex)
    {
        var numVotes = 0;
        foreach (var p in playerManagers)
        {
            if (p.HasBeenVotedForBy(playerIndex)) numVotes++;
            if (numVotes == GetPlayerCount() - 1) return true; // Can't vote for self, hence the -1
        }
        
        Debug.Log($"Hasn't voted for everyone only {numVotes} votes");

        return false;
    }

    private void ResetBonusPointPlacement()
    {
        bonusPointPlacement = new int[7];
        for (int i = 0; i < bonusPointPlacement.Length; i++)
        {
            bonusPointPlacement[i] = -1;
        }
    }

    private int GetNumPlacements()
    {
        int amount = 0;
        foreach (var b in bonusPointPlacement)
        {
            if (b < 0) return amount;
            amount++;
        }

        return amount;
    }

    public bool IsRoundOver()
    {
        return isRoundOver;
    }
/*
    public void CheckEndRoundPlayerLeft()
    {
        if (AreThereEnoughVotesToEndTheRound()) isRoundOver = true;
        RequestSerialization();
        OnDeserialization();
    }*/

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!Networking.LocalPlayer.isMaster) return;
        UpdateIsRoundOver();
    }
}