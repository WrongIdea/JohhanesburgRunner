using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Turns a flat sprite to face the main camera every frame so a
    /// billboarded pickup always shows its front as the player approaches.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        Camera cam;

        void LateUpdate()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null)
                {
                    return;
                }
            }

            Vector3 toObject = transform.position - cam.transform.position;
            if (toObject.sqrMagnitude < 0.0001f)
            {
                return;
            }

            // A SpriteRenderer's face points down local -Z; aim +Z away from
            // the camera so the front turns toward it.
            transform.rotation = Quaternion.LookRotation(toObject, Vector3.up);
        }
    }
}
