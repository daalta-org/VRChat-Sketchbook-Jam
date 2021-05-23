using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PlayerManager : UdonSharpBehaviour
{
    [SerializeField] private PlayerUI playerUI = null;
    [SerializeField] private GameObject stylusGameObject = null;

    private bool isOwner = false;
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
        isOwner = Networking.IsOwner(Networking.LocalPlayer, stylusGameObject);
        playerUI.SetCorrectPrompt(index, isOwner);
    }
}
