using UnityEngine;

public class CameraPivotAligner : MonoBehaviour
{
    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 lookDir = target.position - transform.position;
        lookDir.y = 0;

        if (lookDir.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
