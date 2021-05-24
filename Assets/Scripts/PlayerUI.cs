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
    private readonly int animHashState = Animator.StringToHash("State");

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
            buttonAnimators[i].SetInteger(animHashState, !isOwner ? 0 : index == i ? 2 : 1);
        }
    }

    public void Reset()
    {
        foreach (var t in buttonAnimators)
        {
            t.SetInteger(animHashState, 0);
        }
    }
}
