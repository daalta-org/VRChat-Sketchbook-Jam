using System;
using TMPro;
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

    private bool arePromptsVisible = true;

    public void SetText(string s)
    {
        Debug.Log("Setting text " + s);
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
        texts[1].text = arePromptsVisible ? newText : "";
        texts[2].text = arePromptsVisible ? newText : "";
    }
    public void UpdateAfterWrong()
    {
        texts[0].text = arePromptsVisible ? newText : "";
        texts[2].text = arePromptsVisible ? newText : "";
    }
    public void UpdateAfterCorrect()
    {
        texts[0].text = arePromptsVisible ? newText : "";
        texts[1].text = arePromptsVisible ? newText : "";
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
        Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .1f, .3f, 1f);
        Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .1f, .3f, 1f);
    }

    public void SetPromptsVisible(bool p0)
    {
        arePromptsVisible = p0;
    }
}
