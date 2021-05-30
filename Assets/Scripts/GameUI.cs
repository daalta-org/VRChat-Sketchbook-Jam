using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class GameUI : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI textRound = null;

    [SerializeField] private Animator animatorMusic = null;
    [SerializeField] private Animator[] bonusAnimators = null;
    [SerializeField] private TextMeshProUGUI[] bonusTexts = null;

    [SerializeField] private AnimationCurve curveValue = null;
    [SerializeField] private AnimationCurve curveHue = null;
    [SerializeField] private AnimationCurve curveSkySaturation = null;
    
    private readonly int IsMusicRunning = Animator.StringToHash("IsMusicRunning");
    private readonly int Score = Animator.StringToHash("Score");
    private readonly int IsVoteRevealed = Animator.StringToHash("IsVoteRevealed");
    private readonly int IsVoteSubmitted = Animator.StringToHash("IsVoteSubmitted");
    private readonly int HorizonColor = Shader.PropertyToID("_Color2");
    private readonly int SkyColor = Shader.PropertyToID("_Color1");

    private float skyTimer = -1;
    private bool isSkyAnimating = false;

    private void Start()
    {
        ResetSkyColor();
        skyTimer = 0;
        isSkyAnimating = true;
    }

    private void Update()
    {
        transform.LookAt(Networking.LocalPlayer.GetPosition());
        var rotation = transform.rotation;
        rotation = Quaternion.Euler(rotation.eulerAngles.x, Mathf.Floor((rotation.eulerAngles.y- 22.5f) / 45)*45 + 45  , rotation.eulerAngles.z);
        transform.rotation = rotation;
        
        if (skyTimer < 0) return;
        skyTimer += Time.deltaTime;
        /*
        var modifiedTimer = (skyTimer * 2.75);
        var value = (float) (1 - (((modifiedTimer) / 2) % 1));
        var hue = ((float) modifiedTimer / 16) % 1f;
        Debug.Log(hue);
        */

        var modifiedTimer = skyTimer * 2.75f;

        var skySat = 0;//curveSkySaturation.Evaluate(modifiedTimer);
        if (isSkyAnimating)
        {
            Debug.Log("isSkyAnimating");
            var value = 1;//curveValue.Evaluate(modifiedTimer);
            var hue = modifiedTimer % 1;//curveHue.Evaluate(modifiedTimer);
            RenderSettings.skybox.SetColor(HorizonColor, Color.HSVToRGB(hue, 1, value - skySat * 4, false));
        }
        else
        {
            if (skyTimer > 5f) skyTimer = -1;
        }

        RenderSettings.skybox.SetColor(SkyColor, Color.HSVToRGB(0, 0, skySat, false));
    }

    public void OnRoundChanged(int round)
    {
        textRound.text = "Round " + (round+1);
        if (round >= 0 && round < 5)
        {
            animatorMusic.SetBool(IsMusicRunning, true);
            skyTimer = 0;
            isSkyAnimating = true;
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

    public void OnRoundOver()
    {
        animatorMusic.SetBool(IsMusicRunning, false);
        ResetSkyColor();
        skyTimer = 0;
        isSkyAnimating = false;
    }

    private void ResetSkyColor()
    {
        RenderSettings.skybox.SetColor(HorizonColor, Color.HSVToRGB(.538f, 1, 1, false));
        RenderSettings.skybox.SetColor(SkyColor, Color.HSVToRGB(0, 1, .05f, false));
    }
}
