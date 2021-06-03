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
    private readonly int animatorState = Animator.StringToHash("State");
    private readonly int animatorSpinOnce = Animator.StringToHash("SpinOnce");

    private string newText = "";

    private PlayerManager playerManager = null;
    private int playerIndex = -1;
    private int buttonIndex = -1;

    private bool arePromptsVisible = true;
    private bool arePromptsVisibleOld = false;

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
        texts[1].text = GetText();
        texts[2].text = GetText();
        texts[3].text = GetText();
    }
    public void UpdateAfterWrong()
    {
        texts[0].text = GetText();
        texts[2].text = GetText();
        texts[3].text = GetText();
    }
    public void UpdateAfterCorrect()
    {
        texts[0].text = GetText();
        texts[1].text = GetText();
        texts[3].text = GetText();
    }

    private string GetText()
    {
        return arePromptsVisible ? newText : "";
    }

    public void UpdateAfterDummy()
    {
        texts[0].text = GetText();
        texts[1].text = GetText();
        texts[2].text = GetText();
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
        if (arePromptsVisible != arePromptsVisibleOld)
        {
            arePromptsVisibleOld = arePromptsVisible;
            SpinOnce();
        }
    }
}
