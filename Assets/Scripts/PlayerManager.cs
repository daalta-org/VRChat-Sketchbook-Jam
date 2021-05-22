﻿using UdonSharp;
using UnityEngine;

public class PlayerManager : UdonSharpBehaviour
{
    [SerializeField] private PlayerUI playerUI = null;
    
    private int prompt = -1;
    private int correctIndex = -1;

    public void SetPrompt(int index)
    {
        prompt = index;
        playerUI.SetPrompt(index);
    }

    public void SetCorrectIndex(int index)
    {
        correctIndex = index;
    }
    
}
