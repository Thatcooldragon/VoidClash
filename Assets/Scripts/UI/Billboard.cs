using UnityEngine;

namespace VoidClash
{
    /// <summary>Faces the main camera. Used by health bars and other world-space widgets.</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = cam.transform.rotation;
        }
    }
}
