using TMPro;
using UdonSharp;
using UnityEngine;

public class GameUI : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI textRound = null;

    public void OnRoundChanged(int round)
    {
        textRound.text = "Round " + (round+1);
    }
}
