
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerUI : UdonSharpBehaviour
{
    [SerializeField] private Prompts prompts = null;
    [SerializeField] private TextMeshProUGUI[] text = null;
    [SerializeField] private Button[] button = null;

    private bool localPlayerCanVote = false;
    
    void Start()
    {
        
    }

    public void SetPrompt(int index)
    {
        var prompt = prompts.GetPrompt(index);
        for (var i = 0; i < 7; i++)
        {
            text[i].text = prompt[i];
        }
    }

    public void SetCorrectPrompt(int index, bool isOwner)
    {
        localPlayerCanVote = !isOwner;
        for (var i = 0; i < button.Length; i++)
        {
            button[i].interactable = !isOwner || (isOwner && index == i);
        }
    }
}
