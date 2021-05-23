using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

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

    private int[] promptSequence = null;

    private void Start()
    {
        if (!Networking.IsMaster) return;
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
        UpdatePromptsForPlayers();
        gameUI.OnRoundChanged(round);
    }

    private void UpdatePromptsForPlayers()
    {
        UnityEngine.Random.InitState(seed);
        var promptsThisRound = prompts.GetPromptSequenceForRound(promptSequence, round);
        for (var i = 0; i < playerManagers.Length; i++)
        {
            playerManagers[i].SetPrompt(promptsThisRound[i]);
            playerManagers[i].SetCorrectIndex(UnityEngine.Random.Range(0, 7));
            Debug.Log($"Player {i} received prompt {promptsThisRound[i]}");
        }
    }

    public void ResetAllPlayerManagedPlayedIds(int playerId)
    {
        foreach (var p in playerManagers)
        {
            p.ResetManagedPlayedId(playerId);
        }   
    }
}