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
    [SerializeField] private float jumbleInterval = 2.25f;

    [UdonSynced] private int seed = -1;
    private int seedOld = -1;
    [UdonSynced] private int round = -1;
    private int roundOld = -1;

    public int[] promptSequence = null;

    [UdonSynced] private int playerCount = -1;

    [UdonSynced] private int[] bonusPointPlacement;

    [UdonSynced] private bool isRoundOver = true;

    [UdonSynced] private bool isJumbled = false;
    private bool isJumbledOld = false;
    
    private bool isRoundOverOld = true;

    private float offsetTimer = 0;
    
    private void Start()
    {
        Debug.Log("Executing start event on game manager");
        SetPlayerColors();

        for (var index = 0; index < playerManagers.Length; index++)
        {
            var p = playerManagers[index];
            p.SetButtonInfo(index);
            p.SetPromptsAndGameManager(prompts, this, scoreScript);
        }

        if (!Networking.IsMaster) return;
        ResetBonusPointPlacement();
    }

    private void FixedUpdate()
    {
        if (!isJumbled) return;
        if (isRoundOver || round < 0) return;
        var oldTimer = offsetTimer;
        offsetTimer += Time.fixedDeltaTime;
        if (oldTimer % jumbleInterval > offsetTimer % jumbleInterval)
        {
            SetPromptsForPlayersThisRound((int) (offsetTimer / jumbleInterval), true);
        }
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
        Debug.Log("Game start requested");
        if (round >= 0 && round < 4) return;
        if (!isRoundOver) return;
        if (!Networking.LocalPlayer.isMaster)
        {
            Debug.Log("Non-Master tried to call start game event. That's kinda sus");
            return;
        }
        var numOfPlayers = CountPlayers(false);

        if (numOfPlayers < 3)
        {
            Debug.Log("Player count is below 3.");
            return;
        }
        
        Debug.Log("Game start OK");

        isRoundOver = false;
        seed = Time.frameCount;
        round = 0;
        foreach (var p in playerManagers)
        {
            p.Reset();
        }
        Debug.Log("Requesting Deserialization after game start");
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
        else
        {
            Debug.Log("Seed has not changed and is " + seed);
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

        if (isJumbled != isJumbledOld)
        {
            isJumbledOld = isJumbled;
            gameUI.SetIsJumbled(isJumbled);
            if (!IsRoundOver() && !isJumbled && offsetTimer >= 0)
            {
                SetPromptsForPlayersThisRound(0, false);
                offsetTimer = -1;
            }
        }
        
        UpdateBonusPointUI(); // TODO probably happens too often
    }

    private void OnRoundOver()
    {
        Debug.Log("Round is over!!");
        gameUI.OnRoundOver();
        offsetTimer = -1;
        
        for (var index = 0; index < playerManagers.Length; index++)
        {
            Debug.Log("OnRoundOver for player " + index);

            SetPromptsForPlayersThisRound(0, false);
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

    public void RequestToggleIsJumbled()
    {
        // TODO Non-players should not be able to toggle this
        if (round < 0 || isRoundOver) SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(ToggleIsJumbled));
    }
    
    public void ToggleIsJumbled()
    {
        isJumbled = !isJumbled;
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
        promptSequence = prompts.GetPrompts(seed, 3 * numRounds);
        if (promptSequence.Length != numRounds * 4)
        {
            Debug.LogWarning("prompt sequence has unexpected length: " + promptSequence.Length);
        }
    }

    private void OnRoundChanged()
    {
        if (isJumbled)
        {
            UnityEngine.Random.InitState(seed + round);
            offsetTimer = 0 + UnityEngine.Random.Range(0, 2*jumbleInterval);
        }
        
        SetPromptsForPlayersThisRound((int) (offsetTimer % jumbleInterval), false);
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
    
    private void SetPromptsForPlayersThisRound(int offset, bool preventSelfUpdate)
    {
        Debug.Log("Settings prompts for players for round " + round);
        UnityEngine.Random.InitState(seed);
        var promptsThisRound = prompts.GetPromptSequenceForRound(promptSequence, round);
        var foundMine = false;
        for (var i = 0; i < playerManagers.Length; i++)
        {
            var isMine = false;
            if (!foundMine)
            {
                isMine = playerManagers[i].LocalIsOwner();
                if (preventSelfUpdate && isMine)
                {
                    foundMine = true;
                    continue;
                }
            }
            
            var promptIndex = isMine || !isJumbled ? i : (i + offset) % 3;
            playerManagers[i].SetPrompt(promptsThisRound[promptIndex], offsetTimer >= 0);
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

        for (var i = 0; i < bonusPointPlacement.Length; i++)
        {
            if (bonusPointPlacement[i] < 0)
            {
                bonusPointPlacement[i] = playerIndex;
                Debug.Log($"Player {playerIndex} will get bonus points for finishing!");

                UpdateIsRoundOver(); //isRoundOver = i + 1 >= CountPlayers() - 1; // 2 players (1 + 1) >= 2 players (3 - 1)

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

            if (!isRoundOver) gameUI.MusicDoStageTwo();
            var count = GetPlayerCount();
            if (!isRoundOver && ((count > 3 && i >= count - 3) || i >= count - 2)) gameUI.MusicDoStageThree();
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

    public bool HasVotedForEveryone(int playerIndex)
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
        Debug.Log("Resetting bonus point placement");
        bonusPointPlacement = new int[7];
        for (var i = 0; i < bonusPointPlacement.Length; i++)
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
        foreach (var p in playerManagers)
        {
            p.PlayerHasLeft(player);
        }

        for (var index = 0; index < playerManagers.Length; index++)
        {
            var p = playerManagers[index];
            TryBonusPointPlacement(index); // Later joiners could change the situation...
            p.RequestSerialization();
            p.OnDeserialization();
        }

        UpdateIsRoundOver();
    }
}