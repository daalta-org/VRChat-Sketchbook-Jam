
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerUI : UdonSharpBehaviour
{
    [SerializeField] private Prompts prompts = null;
    [SerializeField] private TextMeshProUGUI[] text = null;
    
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
}
