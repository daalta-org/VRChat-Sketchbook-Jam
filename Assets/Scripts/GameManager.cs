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
}