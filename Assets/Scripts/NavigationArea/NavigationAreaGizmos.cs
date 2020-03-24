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
        var maxIndex = 0;
        for (var i = 0; i < _navPoints.Length; i++)
        {
            if (GetHeap(_navPoints[i]) > GetHeap(_navPoints[maxIndex]))
            {
                maxIndex = i;
            }
        }
        
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                if (_navPoints[i * height + j] != null)
                {
                    //var color = GetCellColorByVisibility(_navPoints[i * height + j]);
                    var color = GetCellColorByHeapMap(_navPoints[i * height + j], GetHeap(_navPoints[maxIndex]));
                    color.a = 0.8f;
                    Gizmos.color = color;
                    //Gizmos.DrawLine(GetLinePosition(_navPoints[i * height + j]), GetLinePosition(_navPoints[i * height + j]) + Vector3.up);
                    Gizmos.DrawCube(_navPoints[i * height + j].position, Vector3.one * _meshStep * 0.9f);
                }
            }
        }
    }

    private float GetHeap(NavigationPoint point)
    {
        //return point.distanceToPlayerWeight;
        //return point.distanceToAgentWeight;
        if (!point.isPassable) return 0;
        //return ComputeCost(point, _agent);
        return point.distanceToAgentWeight + point.distanceToPlayerWeight * 2;
    }

    private Color GetCellColorByVisibility(NavigationPoint point)
    {
        if (!point.isPassable)
        {
            return Color.black;
        }
        if (PointIsVisibleFromPlayerPosition(point))
        {
            return Color.magenta;
        }
        return Color.white;
    }

    private Color GetCellColorByHeapMap(NavigationPoint point, float maxValue)
    {
        if (!point.isPassable)
        {
            return Color.black;
        }
        if (PointIsVisibleFromPlayerPosition(point))
        {
            return Color.magenta;
        }
        var t = GetHeap(point) / (float)maxValue;
        return Color.green * (1 - t) + Color.red * t;
    }
}
