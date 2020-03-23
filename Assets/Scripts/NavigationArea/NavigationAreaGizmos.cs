using UnityEngine;

public partial class NavigationArea : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        if (_navPoints == null)
        {
            Debug.Log("NavPoints array is null");
            return;
        }
        UpdateWeights();
        
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                if (_navPoints[i * height + j] != null)
                {
                    var color = GetColor(_navPoints[i * height + j]);
                    color.a = 0.8f;
                    Gizmos.color = color;
                    //Gizmos.DrawLine(GetLinePosition(_navPoints[i * height + j]), GetLinePosition(_navPoints[i * height + j]) + Vector3.up);
                    Gizmos.DrawCube(_navPoints[i * height + j].position, Vector3.one * _meshStep * 0.9f);
                }
            }
        }
    }
        
    private Color GetColor(NavigationPoint point)
    {
        if (!point.isPassable)
        {
            return Color.black;
        }
        if (point.weight >= 10000)
        {
            return Color.magenta;
        }
        if (point.weight < 10000)
        {
            return Color.white;
        }
        return Color.white;
    }
}
