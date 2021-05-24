using TMPro;
using UdonSharp;
using UnityEngine;

public class ButtonManager : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI[] texts = null;
    [SerializeField] private Animator animator = null;
    private readonly int animatorState = Animator.StringToHash("State");

    public void SetText(string newText)
    {
        foreach (var text in texts)
        {
            text.text = newText;
        }
    }

    public void SetAnimatorState(int state)
    {
        animator.SetInteger(animatorState, state);
    }
    
    private void OnAnimatorMove() 
    {
        if (animator != null)
        {
            transform.SetPositionAndRotation(animator.targetPosition, animator.targetRotation);
        }
    }
}
