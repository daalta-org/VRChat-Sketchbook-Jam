
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PenLineColorSharp : UdonSharpBehaviour
{
    [UdonSynced]
    private Vector3[] points;
    private LineRenderer lineRenderer;
    private Mesh lineMesh;
    private MeshCollider lineCollider;
    public Color color;

    void Start()
    {
        //Cache LineRenderer
        LineRenderer temp = GetComponent<LineRenderer>();
        if (Utilities.IsValid(temp))
        {
            lineRenderer = temp;
        }
        else
        {
            Debug.Log("Couldn't find required component of type LineRenderer on " + gameObject.name);
        }

        //Cache MeshCollider
        MeshCollider tempMesh = GetComponent<MeshCollider>();
        if (Utilities.IsValid(tempMesh))
        {
            lineCollider = tempMesh;
        }
        else
        {
            Debug.Log("Couldn't find required component of type MeshCollider on " + gameObject.name);
        }

        lineMesh = new Mesh();
    }

    public void OnUpdate() //OnUpdate event, set points from current line positions, check for color changes
    {
        points = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(points);
        RequestSerialization();
        lineRenderer.enabled = true;
    }
    
    public void UpdateLineColor()
    {
        lineRenderer.startColor = color;
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        lineRenderer.endColor = Color.HSVToRGB(h - 0.1f, s, v);
    }

    public void OnFinish()
    {
        lineRenderer.Simplify(0.005f);
        OnUpdate();
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BakeMesh));
    }

    public void BakeMesh()
    {
        lineRenderer.BakeMesh(lineMesh, false);
        lineCollider.sharedMesh = lineMesh;
    }

    public override void OnDeserialization()
    {
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineRenderer.enabled = true;
    }

    public void Erase()
    {
        if (Networking.IsOwner(gameObject))
        {
            lineRenderer.positionCount = 0;
            OnUpdate();
        }
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
    {
        return true;
    }
}
