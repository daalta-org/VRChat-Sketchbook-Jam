﻿using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI textInstruction = null;
    [SerializeField] private ButtonManager[] buttonManagers = null;
    [SerializeField] private MeshRenderer[] meshRenderersColor = null;
    [SerializeField] private Material[] materialsColor = null;
    [SerializeField] private Animator[] animatorsVoteResult = null;

    [Header("Strings")] 
    [SerializeField] private string stringJoin = "<b>Join!</b>\n<size=60%>Pick up a pen to play</size>";
    [SerializeField] private string stringOccupied = "<b>{0}</b>\n<size=60%>Is ready to play!</size>";
    [SerializeField] private string stringEmptyGameRunning = " \n<size=60%>Empty, please join the next game!</size>";
    [SerializeField] private string stringVoteSubmitted= "<b>Voted!</b>\n<size=60%>Keep guessing other drawings!\nGet points for guessing fast!</size>";
    [SerializeField] private string stringVoteGuess= "<b>Guess!</b>\n<size=60%>Click to guess what {0} is drawing!\nYou only get 1 vote!</size>";
    [SerializeField] private string stringDraw = "<b>Draw!</b>\n<size=60%>Draw the green prompt!\nGet points when players guess it!</size>";

    public void SetButtonInfo(PlayerManager pm)
    {
        for (var index = 0; index < buttonManagers.Length; index++)
        {
            var buttonManager = buttonManagers[index];
            buttonManager.SetButtonInfo(pm, index);
        }
    }
    
    public void SetColor(int colorIndex)
    {
        foreach (var meshRenderer in meshRenderersColor)
        {
            meshRenderer.material = materialsColor[colorIndex];
        }
    }

    public void SetPrompt(int index, Prompts prompts)
    {
        var prompt = prompts.GetPrompt(index);
        for (var i = 0; i < 6; i++)
        {
            buttonManagers[i].SetText(prompt[i]); // Object reference not set error
        }
    }

    public void SetPromptCorrect(int index)
    {
        SetPromptState(index, 2);
    }
    
    public void SetPromptWrong(int index)
    {
        SetPromptState(index, 1);
    }
        
    public void SetPromptNeutral(int index)
    {
        SetPromptState(index, 0);
    }

    public void SetPromptState(int index, int value)
    {
        buttonManagers[index].SetAnimatorState(value);
    }

    public void ResetAnimatorState()
    {
        foreach (var t in buttonManagers)
        {
            t.SetAnimatorState(0);
        }
    }

    public void MakeAllPromptsNeutral()
    {
        foreach (var buttonManager in buttonManagers)
        {
            buttonManager.SetAnimatorState(0);
        }

        HideVoteResults(); // TODO This might not belong here. Probably called too often.
    }

    public void UpdateInstructions(int round, string ownerName, bool isOwner, bool hasVoted)
    {
        string s;
        if (round < 0)
        {
            if (ownerName == null)
            {
                s = stringJoin;
            }
            else
            {
                s = string.Format(stringOccupied, ownerName);
            }
        }
        else if (ownerName == null)
        {
            s = stringEmptyGameRunning;
        }
        else if (isOwner)
        {
            s = stringDraw;
        }
        else
        {
            s = !hasVoted ? string.Format(stringVoteGuess, ownerName) : stringVoteSubmitted;
        }

        textInstruction.text = s;
    }

    public void ClearText()
    {
        for (var i = 0; i < 6; i++)
        {
            buttonManagers[i].SetText("");
        }
    }

    public void SetVoteResults(int[] votes, bool isRevealed, int[] scoresVote)
    {
        var scoreIndex = 0;
        for (var i = 0; i < animatorsVoteResult.Length; i++)
        {
            var isSubmitted = votes[i] > -1;
            if (isSubmitted)
            {
                var score = 0;
                var isCorrect = votes[i] < 10;
                if (isCorrect)
                {
                    score = scoresVote[scoreIndex];
                    scoreIndex++;
                }
                animatorsVoteResult[i].SetInteger("Score", score); // TODO placeholder score
                animatorsVoteResult[i].SetBool("IsVoteRevealed", isRevealed);
            }

            animatorsVoteResult[i].SetBool("IsVoteSubmitted", isSubmitted);
        }
    }

    public void HideVoteResults()
    {
        for (int i = 0; i < animatorsVoteResult.Length; i++)
        {
            animatorsVoteResult[i].SetInteger("Score", -1);
            animatorsVoteResult[i].SetBool("IsVoteRevealed", false);
            animatorsVoteResult[i].SetBool("IsVoteSubmitted", false);
        }
    }
}
