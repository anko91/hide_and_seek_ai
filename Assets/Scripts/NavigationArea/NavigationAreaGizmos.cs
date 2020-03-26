using UnityEngine;

public partial class NavigationArea : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        RaycastCheck();
        DrawUnPassableAreas();
    }

    private void RaycastCheck()
    {
        var origin = _player.position;
        for (var i = 0; i < 360; i++)
        {
            var direction = new Vector3(Mathf.Sin(i), 0, Mathf.Cos(i));
            RaycastHit hit;
            var distance = _playerLookRadius;
            if (Physics.Raycast(origin, direction, out hit, _playerLookRadius, _player.gameObject.layer))
            {
                distance = hit.distance;
            }
            Debug.DrawRay(origin, direction * distance, Color.magenta);
        }
    }

    private void DrawUnPassableAreas()
    {
        for (var i = 0; i < _navPoints.Length; i++)
        {
            if (!_navPoints[i].isPassable)
            {
                var color = Color.black;
                color.a = 0.8f;
                Gizmos.color = color;
                Gizmos.DrawCube(_navPoints[i].position, Vector3.one * _meshStep * 0.9f);
            }
        }
    }
}
