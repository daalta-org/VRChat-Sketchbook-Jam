using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public class GameManager : UdonSharpBehaviour
{
    public GameObject myPen = null;

    [SerializeField] private GameUI gameUI = null;
    [SerializeField] private PlayerManager[] playerManagers = null;
    [SerializeField] private Prompts prompts = null;
    [SerializeField] private int numRounds = 5;

    [UdonSynced] private int seed = -1;
    private int seedOld = -1;
    [UdonSynced] private int round = -1;
    private int roundOld = -1;

    public int[] promptSequence = null;

    private void Start()
    {
        foreach (var p in playerManagers)
        {
            p.SetPromptsAndGameManager(prompts, this);
        }
        
        if (!Networking.IsMaster) return;
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

    public void NextRound()
    {
        round++;
        OnRoundChanged();
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
        gameUI.OnRoundChanged(round);
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
}