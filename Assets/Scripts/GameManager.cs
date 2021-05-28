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

    private void Start()
    {
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

    public void StartGame()
    {
        if (!Networking.LocalPlayer.isMaster) return;
        seed = UnityEngine.Random.Range(-int.MaxValue, int.MaxValue);
        round = 0;
        Debug.Log("Requesting Deserialization...");
        RequestSerialization();
        OnDeserialization();
    }

    public int GetPlayerCount()
    {
        return playerCount;
    }

    public override void OnDeserialization()
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

        UpdateBonusPointUI(); // TODO probably happens too often
    }

    public void RequestNextRound()
    {
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(NextRound));
        Debug.Log("Requesting that the master progresses to the next round...");
    }
    
    public void NextRound()
    {
        Debug.Log("Master: Increasing round counter and sending it to clients.");
        round++;
        RequestSerialization();
        OnDeserialization();
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

        playerCount = CountPlayers();

        gameUI.OnRoundChanged(round);
    }

    private int CountPlayers()
    {
        var count = 0;
        foreach (var p in playerManagers)
        {
            if (p.GetIsPlaying()) count++;
        }

        return count;
    }
    
    private void SetPromptsForPlayersThisRound()
    {
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

        for (var i = 0; i < bonusPointPlacement.Length; i++)
        {
            if (bonusPointPlacement[i] < 0)
            {
                bonusPointPlacement[i] = playerIndex;
                Debug.Log($"Player {playerIndex} got bonus points for finishing!");
                RequestSerialization();
                OnDeserialization();
                return;
            }
        }
    }

    private void UpdateBonusPointUI()
    {
        var arrayLen = 0;
        foreach (var b in bonusPointPlacement)
        {
            if (b == -1) break;
            arrayLen++;
        }
        
        var points = new int[arrayLen];
        var names = new string[arrayLen];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = scoreScript.GetBonusPoints(playerCount, i);
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
    /// Gives each player the bonus points depending on their placement.
    /// </summary>
    private void FinalizeAndGiveBonusPoints()
    {
        for (var index = 0; index < bonusPointPlacement.Length; index++)
        {
            var playerIndex = bonusPointPlacement[index];
            playerManagers[playerIndex].AddPoints(scoreScript.GetBonusPoints(playerIndex, index));
        }
    }

    private bool HasVotedForEveryone(int playerIndex)
    {
        var numVotes = 0;
        foreach (var p in playerManagers)
        {
            if (p.HasBeenVotedForBy(playerIndex)) numVotes++;
            if (numVotes == playerCount - 1) return true; // Can't vote for self, hence the -1
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
}