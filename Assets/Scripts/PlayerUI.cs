using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : UdonSharpBehaviour
{
    [SerializeField] private ButtonManager[] buttonManagers = null;

    private readonly int animHashState = Animator.StringToHash("State");

    public void SetPrompt(int index, Prompts prompts)
    {
        var prompt = prompts.GetPrompt(index);
        for (var i = 0; i < 7; i++)
        {
            buttonManagers[i].SetText(prompt[i]); // Object reference not set error
        }
    }

    public void SetCorrectPrompt(int index, bool isOwner)
    {
        Debug.Log($"Setting correct index {index} for " + (isOwner ? "owner" : "not owner"));
        for (var i = 0; i < buttonManagers.Length; i++)
        {
            buttonManagers[i].SetAnimatorState(!isOwner ? 0 : index == i ? 2 : 1);
        }
    }

    public void ResetAnimatorState()
    {
        foreach (var t in buttonManagers)
        {
            t.SetAnimatorState(0);
        }
    }
}
