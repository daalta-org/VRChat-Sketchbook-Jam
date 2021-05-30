﻿using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class ButtonManager : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI[] texts = null;
    [SerializeField] private Animator animator = null;
    [SerializeField] private Button neutralButton = null;
    private readonly int animatorState = Animator.StringToHash("State");
    private readonly int animatorSpinOnce = Animator.StringToHash("SpinOnce");

    private string newText = "";

    private PlayerManager playerManager = null;
    private int playerIndex = -1;
    private int buttonIndex = -1;
    
    public void SetText(string s)
    {
        newText = s;
        SpinOnce();
    }

    public void SetAnimatorState(int state)
    {
        SpinOnce();
        animator.SetInteger(animatorState, state);
    }

    private void SpinOnce()
    {
        animator.SetTrigger(animatorSpinOnce);
    }

    public void UpdateAfterNeutral()
    {
        texts[1].text = newText;
        texts[2].text = newText;
    }
    public void UpdateAfterWrong()
    {
        texts[0].text = newText;
        texts[2].text = newText;
    }
    public void UpdateAfterCorrect()
    {
        texts[0].text = newText;
        texts[1].text = newText;
    }

    public void OnButtonPressed()
    {
        playerManager.OnButtonPressed(buttonIndex);
    }

    public void SetButtonInfo(PlayerManager pm, int bi)
    {
        playerManager = pm;
        buttonIndex = bi;
    }

    public void OnHover()
    {
        Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, default, default, default);
        Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, default, default, default);
    }
}
