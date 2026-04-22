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
    [Tooltip("Minimum wall clearance even when near clip is tiny.")]
    [SerializeField] private float minWallClearance = 0.08f;
    [Tooltip("Radius for sphere cast (more reliable than a thin ray).")]
    [SerializeField] private float castRadius = 0.12f;
    [Tooltip("Layers to check for collision.")]
    [SerializeField] private LayerMask collisionLayers = -1;
    [Header("Distance Smoothing")]
    [Tooltip("How fast camera moves inward when obstruction appears.")]
    [SerializeField] private float obstructionPullSpeed = 20f;
    [Tooltip("How fast camera returns outward when obstruction clears.")]
    [SerializeField] private float obstructionReleaseSpeed = 12f;
    private Transform cameraHolder;
    private Transform playerRoot;
    private Vector3 defaultLocalPos;
    private Camera cam;
    private PhotonView photonView;
    private float smoothedDistance = -1f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cameraHolder = transform.parent;
        if (cameraHolder != null)
            defaultLocalPos = transform.localPosition;
        if (cameraHolder != null)
            playerRoot = cameraHolder.parent;
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

        // Keep enough room for near clip without aggressively pushing camera around.
        float minSafeMargin = Mathf.Max(minWallClearance, cam.nearClipPlane * 1.15f);
        float effectiveMargin = Mathf.Max(clipMargin, minSafeMargin);

        RaycastHit[] hits = Physics.SphereCastAll(origin, castRadius, direction, distance, collisionLayers);
        float closestHitDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (IsSelfCollider(hit.collider))
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
                        if (IsSelfCollider(viewHit.collider))
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

        float targetDistance = Mathf.Clamp(Vector3.Dot(cameraPos - origin, direction), 0f, distance);
        if (smoothedDistance < 0f)
            smoothedDistance = targetDistance;

        bool gettingCloser = targetDistance < smoothedDistance;
        float speed = gettingCloser ? Mathf.Max(0.01f, obstructionPullSpeed) : Mathf.Max(0.01f, obstructionReleaseSpeed);
        smoothedDistance = Mathf.MoveTowards(smoothedDistance, targetDistance, speed * Time.deltaTime);

        transform.position = origin + direction * smoothedDistance;
    }

    private bool IsSelfCollider(Collider col)
    {
        if (col == null) return false;
        Transform t = col.transform;
        if (t == transform || t.IsChildOf(transform))
            return true;
        if (playerRoot != null && (t == playerRoot || t.IsChildOf(playerRoot)))
            return true;
        if (photonView != null && col.GetComponentInParent<PhotonView>() == photonView)
            return true;
        return false;
    }
}
