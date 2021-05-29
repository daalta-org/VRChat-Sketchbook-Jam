
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

    private bool isLineRendererSet = false;
    
    void Start()
    {
        GetLineRenderer();
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

    private LineRenderer GetLineRenderer()
    {
        if (isLineRendererSet) return lineRenderer;
        
        //Cache LineRenderer
        LineRenderer temp = GetComponent<LineRenderer>();
        if (Utilities.IsValid(temp))
        {
            lineRenderer = temp;
            isLineRendererSet = true;
            return lineRenderer;
        }
        else
        {
            Debug.Log("Couldn't find required component of type LineRenderer on " + gameObject.name);
        }

        return null;
    }

    public void OnUpdate() //OnUpdate event, set points from current line positions, check for color changes
    {
        points = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(points);
        RequestSerialization();
        lineRenderer.enabled = true;
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

    public void SetColor(Color color)
    {
        GetLineRenderer().startColor = color;
        GetLineRenderer().endColor = color;
    }
}
