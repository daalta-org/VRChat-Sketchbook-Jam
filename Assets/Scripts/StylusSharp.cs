
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class StylusSharp : UdonSharpBehaviour
{
    private PenLineColorSharp line;
    private bool isDrawing;
    private Vector3 startPosition;
    public float minMoveDistance = 0.01f;
    private Vector3[] points;
    private LineRenderer lineRenderer = null;
    public Transform penTip;
    private int currentIndex = -1;
    public int pointsPerUpdate = 10;
    public Transform lineContainer;
    [UdonSynced]
    private int nextLineIndex;
    private GameObject[] pool;
    [SerializeField] private LineRenderer[] linePool;
    [SerializeField] private PlayerManager playerManager = null;
    [SerializeField] private PenLineColorSharp[] penLineColorSharp = null;

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
    {
        return true;
    }

    public override void OnPickupUseDown()
    {
        lineRenderer = linePool[nextLineIndex];
        var temp = lineRenderer.GetComponent<PenLineColorSharp>();
        if (Utilities.IsValid(temp))
        {
            line = temp;
        }
        Networking.SetOwner(Networking.LocalPlayer, lineRenderer.gameObject);
        lineRenderer.gameObject.SetActive(true);

        nextLineIndex = (nextLineIndex + 1) % linePool.Length;
        
        line.RequestSerialization();

        isDrawing = true;
        lineRenderer.positionCount = 2;
        startPosition = penTip.position;
        currentIndex = 0;

        for(var i = 0; i < 2; i++)
        {
            lineRenderer.SetPosition(i, penTip.position);
        }
    }

    public void Update()
    {
        if (!isDrawing) return;
        if (!(Vector3.Distance(penTip.position, startPosition) > minMoveDistance)) return;
        lineRenderer.positionCount = currentIndex + 1;
        lineRenderer.SetPosition(currentIndex, penTip.position);
        startPosition = penTip.position;
        currentIndex++;
        if((currentIndex & pointsPerUpdate) == 0)
        {
            line.OnUpdate();
        }
    }

    public override void OnPickupUseUp()
    {
        isDrawing = false;
        line.OnFinish();
    }

    public override void OnPickup()
    {
        playerManager.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(playerManager.UpdateOwnerID));
    }

    public void Erase()
    {
        foreach (var p in penLineColorSharp)
        {
            p.Erase();
        }
    }
}
