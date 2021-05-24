using TMPro;
using UdonSharp;
using UnityEngine;

public class GameUI : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI textRound = null;

    [SerializeField] private Animator animatorMusic = null;
    private readonly int IsMusicRunning = Animator.StringToHash("IsMusicRunning");

    public void OnRoundChanged(int round)
    {
        textRound.text = "Round " + (round+1);
        if (round >= 0 && round < 5)
        {
            animatorMusic.SetBool(IsMusicRunning, true);
        }
    }
}
