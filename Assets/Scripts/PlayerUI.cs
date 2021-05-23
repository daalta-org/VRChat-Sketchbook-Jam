using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI[] text = null;
    [SerializeField] private Animator[] buttonAnimators = null;

    private bool localPlayerCanVote = false;
    private readonly int IsCorrect = Animator.StringToHash("IsCorrect");
    private readonly int IsNeutral = Animator.StringToHash("IsNeutral");

    public void SetPrompt(int index, Prompts prompts)
    {
        var prompt = prompts.GetPrompt(index);
        for (var i = 0; i < 7; i++)
        {
            text[i].text = prompt[i]; // Object reference not set error
        }
    }

    public void SetCorrectPrompt(int index, bool isOwner)
    {
        localPlayerCanVote = !isOwner;
        for (var i = 0; i < buttonAnimators.Length; i++)
        {
            if (isOwner && index == i)
            {
                buttonAnimators[i].SetTrigger(IsCorrect);
            }
            else
            {
                buttonAnimators[i].SetTrigger(IsNeutral);
            }
        }
    }

    public void Reset()
    {
        foreach (var t in buttonAnimators)
        {
            t.SetTrigger(IsNeutral);
        }
    }
}
