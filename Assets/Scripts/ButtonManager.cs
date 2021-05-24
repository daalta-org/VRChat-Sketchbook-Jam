using TMPro;
using UdonSharp;
using UnityEngine;

public class ButtonManager : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI[] texts = null;
    [SerializeField] private Animator animator = null;
    private readonly int animatorState = Animator.StringToHash("State");
    private readonly int animatorSpinOnce = Animator.StringToHash("SpinOnce");

    private string newText = ""; 
    
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
}
