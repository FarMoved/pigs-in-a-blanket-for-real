using UnityEngine;
using Photon.Pun;

/// <summary>
/// Stops the camera from sitting inside walls/blocks so you never see through geometry.
/// When something is between the player and the camera, the camera is pulled back so it
/// stays in front of the surface. Margin is kept at least as large as the camera near
/// clip plane so nothing is clipped away.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraCollisionHandler : MonoBehaviour
{
    [Header("Collision")]
    [Tooltip("Optional: ray starts here. If not set, uses player root + height offset.")]
    [SerializeField] private Transform rayOrigin;
    [Tooltip("When rayOrigin is not set: height above player root to start the ray.")]
    [SerializeField] private float rayOriginHeight = 0.5f;
    [Tooltip("Extra distance to keep from walls. Automatically at least the camera near clip plane.")]
    [SerializeField] private float clipMargin = 0.05f;
    [Tooltip("Radius for sphere cast (more reliable than a thin ray).")]
    [SerializeField] private float castRadius = 0.12f;
    [Tooltip("Layers to check for collision.")]
    [SerializeField] private LayerMask collisionLayers = -1;

    private Transform cameraHolder;
    private Vector3 defaultLocalPos;
    private Camera cam;
    private PhotonView photonView;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cameraHolder = transform.parent;
        if (cameraHolder != null)
            defaultLocalPos = transform.localPosition;
        photonView = GetComponentInParent<PhotonView>();
    }

    private void LateUpdate()
    {
        if (cam == null || !cam.enabled || cameraHolder == null) return;
        if (photonView != null && !photonView.IsMine) return;

        Vector3 desiredWorldPos = cameraHolder.TransformPoint(defaultLocalPos);
        Vector3 origin;
        if (rayOrigin != null)
            origin = rayOrigin.position;
        else
        {
            Transform playerRoot = cameraHolder.parent;
            if (playerRoot == null) return;
            origin = playerRoot.position + Vector3.up * rayOriginHeight;
        }

        Vector3 direction = desiredWorldPos - origin;
        float distance = direction.magnitude;
        if (distance < 0.001f) return;

        direction /= distance;

        // Stay well back from walls so the near clip plane never cuts into geometry (no see-through).
        // Use a fixed minimum distance so blocks never look translucent.
        float minSafeMargin = Mathf.Max(0.75f, cam.nearClipPlane * 2.5f);
        float effectiveMargin = Mathf.Max(clipMargin, minSafeMargin);

        RaycastHit[] hits = Physics.SphereCastAll(origin, castRadius, direction, distance, collisionLayers);
        float closestHitDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (photonView != null && hit.collider.GetComponentInParent<PhotonView>() == photonView)
                continue;
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;
            if (hit.distance < closestHitDistance)
                closestHitDistance = hit.distance;
        }

        Vector3 cameraPos;
        if (closestHitDistance < float.MaxValue)
        {
            float pullDistance = Mathf.Max(0f, closestHitDistance - effectiveMargin);
            cameraPos = origin + direction * pullDistance;
        }
        else
        {
            cameraPos = desiredWorldPos;
        }

        // Check multiple directions (center + 4 corners of view) so we never see through when turning.
        // Pull the camera toward the player until every checked direction has clearance.
        Vector3 toPlayer = origin - cameraPos;
        float toPlayerDist = toPlayer.magnitude;
        if (toPlayerDist > 0.001f)
        {
            Vector3 toPlayerDir = toPlayer / toPlayerDist;
            Vector3 look = cameraHolder.forward;
            Vector3 right = cameraHolder.right;
            Vector3 up = cameraHolder.up;
            float halfFovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            // Directions: center, and 4 corners (cover the view frustum)
            float corner = Mathf.Tan(halfFovRad) * 0.85f;
            Vector3[] viewDirs = new Vector3[]
            {
                look,
                (look + right * corner + up * corner).normalized,
                (look - right * corner + up * corner).normalized,
                (look + right * corner - up * corner).normalized,
                (look - right * corner - up * corner).normalized
            };

            const int maxIterations = 8;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                float maxPull = 0f;
                foreach (Vector3 dir in viewDirs)
                {
                    if (Physics.SphereCast(cameraPos, castRadius * 0.5f, dir, out RaycastHit viewHit, effectiveMargin * 3f, collisionLayers))
                    {
                        if (viewHit.collider.transform == transform || viewHit.collider.transform.IsChildOf(transform))
                            continue;
                        if (photonView != null && viewHit.collider.GetComponentInParent<PhotonView>() == photonView)
                            continue;
                        // Require full margin + small buffer so the near plane never clips into the block
                        float requiredDist = effectiveMargin * 1.15f;
                        if (viewHit.distance < requiredDist)
                        {
                            float pull = requiredDist - viewHit.distance;
                            if (pull > maxPull) maxPull = pull;
                        }
                    }
                }
                if (maxPull <= 0f) break;
                cameraPos += toPlayerDir * maxPull;
                toPlayerDist -= maxPull;
                if (toPlayerDist <= 0f) break;
                toPlayerDir = (origin - cameraPos).normalized;
            }
        }

        transform.position = cameraPos;
    }
}
