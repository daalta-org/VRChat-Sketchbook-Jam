using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : UdonSharpBehaviour
{
    [SerializeField] private ButtonManager[] buttonManagers = null;
    [SerializeField] private MeshRenderer[] meshRenderersColor = null;
    [SerializeField] private Material[] materialsColor = null;

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
    }
}
