using UnityEngine;
using System.Diagnostics;

public class HierarchyStalker : MonoBehaviour
{
    private void OnDisable()
    {
        UnityEngine.Debug.Log($"<color=red>[Stalker]</color> <b>{gameObject.name}</b> IS BEING DISABLED!");
        
        // ดึง StackTrace เพื่อดูว่าใครเป็นคนสั่ง
        StackTrace stackTrace = new StackTrace(true);
        UnityEngine.Debug.Log($"<color=orange>[Stalker]</color> Call Stack:\n{stackTrace.ToString()}");
    }

    private void OnDestroy()
    {
        UnityEngine.Debug.Log($"<color=red>[Stalker]</color> <b>{gameObject.name}</b> IS BEING DESTROYED!");
        StackTrace stackTrace = new StackTrace(true);
        UnityEngine.Debug.Log($"<color=orange>[Stalker]</color> Call Stack:\n{stackTrace.ToString()}");
    }
}
