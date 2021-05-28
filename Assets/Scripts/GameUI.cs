using TMPro;
using UdonSharp;
using UnityEngine;

public class GameUI : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI textRound = null;

    [SerializeField] private Animator animatorMusic = null;
    [SerializeField] private Animator[] bonusAnimators = null;
    [SerializeField] private TextMeshProUGUI[] bonusTexts = null;
    
    private readonly int IsMusicRunning = Animator.StringToHash("IsMusicRunning");
    private readonly int Score = Animator.StringToHash("Score");
    private readonly int IsVoteRevealed = Animator.StringToHash("IsVoteRevealed");
    private readonly int IsVoteSubmitted = Animator.StringToHash("IsVoteSubmitted");
    public void OnRoundChanged(int round)
    {
        textRound.text = "Round " + (round+1);
        if (round >= 0 && round < 5)
        {
            animatorMusic.SetBool(IsMusicRunning, true);
        }
    }

    public void SetBonusPoints(int[] points, string[] names)
    {
        Debug.Log("Points length " + points.Length);

        if (points.Length == 0)
        {
            ResetBonusPoints();
            return;
        }
        
        for (var i = 0; i < bonusAnimators.Length; i++)
        {
            Debug.Log(i);
            var isValid = i < points.Length;
            bonusAnimators[i].SetInteger(Score, isValid ? points[i] : -1);
            bonusAnimators[i].SetBool(IsVoteRevealed, true);
            bonusAnimators[i].SetBool(IsVoteSubmitted, true);
            bonusTexts[i].text = isValid ? names[i] : "";
        }
    }

    public void ResetBonusPoints()
    {
        for (int i = 0; i < bonusAnimators.Length; i++)
        {
            bonusAnimators[i].SetBool(IsVoteRevealed, false);
            bonusAnimators[i].SetBool(IsVoteSubmitted, false);
            bonusTexts[i].text = "";
        }
    }
}
