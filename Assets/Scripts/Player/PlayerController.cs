using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

/// <summary>
/// First-person player controller handling movement, jumping, and camera look.
/// Networked via Photon - only processes input for local player.
/// </summary>
public class PlayerController : MonoBehaviourPunCallbacks
{
    private const string CrouchPropertyKey = "Crouch";

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -40f;
    [SerializeField] private float crouchHeightPercent = 0.45f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float standingHitboxScale = 0.58f;
    [SerializeField] private float crouchHitboxScale = 0.36f;
    [SerializeField] private float standingHeadTopPadding = 0.02f;
    [SerializeField] private float crouchVisualScaleY = 0.8f;
    [SerializeField] private float crouchVisualDrop = 0.12f;
    [SerializeField] private float crouchTransitionSpeed = 12f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Movement Ability Prototypes")]
    [SerializeField] private bool enableSizzleStep = true;
    [SerializeField] private bool enableWallSkim = true;
    [SerializeField] private bool enableLaunchPatty = false;
    [SerializeField] private KeyCode sizzleStepKey = KeyCode.Q;
    [SerializeField] private KeyCode wallSkimKey = KeyCode.E;
    [SerializeField] private KeyCode launchPattyKey = KeyCode.F;
    [SerializeField] private float sizzleStepDistance = 7f;
    [SerializeField] private float sizzleStepCooldown = 2.25f;
    [SerializeField] private float wallSkimDuration = 2.1f;
    [SerializeField] private float wallSkimCooldown = 3f;
    [SerializeField] private float wallSkimRiseSpeed = 3.5f;
    [SerializeField] private float wallSkimFallSpeed = -2f;
    [SerializeField] private float wallDetectDistance = 1.05f;
    [SerializeField] private float wallSkimMomentumRampPerSecond = 1.35f;
    [SerializeField] private float wallSkimComboBoost = 0.55f;
    [SerializeField] private float wallSkimMaxMomentumMultiplier = 5.1f;
    [SerializeField] private float wallSkimMomentumDecay = 2.25f;
    [SerializeField] private float wallSkimWallSwitchDotThreshold = 0.55f;
    [SerializeField] private float launchPattyHeight = 3.8f;
    [SerializeField] private float launchPattyForwardBoost = 5.25f;
    [SerializeField] private float launchPattyCooldown = 4f;
    [SerializeField] private LayerMask wallSkimLayers = ~0;
    [Header("Ability Debug HUD")]
    [SerializeField] private bool showAbilityDebugHud = true;
    [SerializeField] private Vector2 abilityDebugHudScreenOffset = new Vector2(20f, 170f);

    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CapsuleCollider hitboxCollider;
    [SerializeField] private Transform visualModelRoot;
    [SerializeField] private Transform headTransform;

    // Private state
    private Vector3 velocity;
    private float verticalRotation = 0f;
    private bool isGrounded;
    private float standingHeight;
    private Vector3 standingCenter;
    private float standingHitboxHeight;
    private Vector3 standingHitboxCenter;
    private float standingCameraY;
    private Vector3 standingVisualLocalPosition;
    private Vector3 standingVisualLocalScale;
    private bool isCrouching;
    private float crouchBlend;
    private float crouchBlendTarget;
    private bool lastSentCrouchState;
    private float sizzleStepCooldownTimer;
    private float wallSkimCooldownTimer;
    private float wallSkimTimer;
    private float launchPattyCooldownTimer;
    private Vector2 cachedMoveInput;
    private float wallSkimMomentumMultiplier = 1f;
    private bool hasLastWallNormal;
    private Vector3 lastWallNormal;

    private void Start()
    {
        // Only control the local player
        if (photonView.IsMine)
        {
            // Lock and hide cursor for FPS controls
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Enable the camera only for local player and tag as MainCamera so weapons work
            if (cameraHolder != null)
            {
                Camera cam = cameraHolder.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cam.enabled = true;
                    cam.tag = "MainCamera";
                }
            }
        }
        else
        {
            // Disable camera and AudioListener for remote players (avoids "2 audio listeners" warning)
            if (cameraHolder != null)
            {
                Camera cam = cameraHolder.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cam.enabled = false;
                    AudioListener listener = cam.GetComponent<AudioListener>();
                    if (listener != null)
                        listener.enabled = false;
                }
            }
        }
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (hitboxCollider == null)
            hitboxCollider = GetComponent<CapsuleCollider>();
        if (characterController != null)
        {
            standingHeight = characterController.height;
            standingCenter = characterController.center;
        }
        if (hitboxCollider != null)
        {
            standingHitboxHeight = hitboxCollider.height;
            standingHitboxCenter = hitboxCollider.center;
            if (standingHeight > 0f && standingHitboxHeight > standingHeight * 1.25f)
            {
                // Clamp oversized root hitbox to match the movement capsule.
                standingHitboxHeight = standingHeight;
                standingHitboxCenter = standingCenter;
                hitboxCollider.height = standingHitboxHeight;
                hitboxCollider.center = standingHitboxCenter;
            }
        }
        if (cameraHolder != null)
            standingCameraY = cameraHolder.localPosition.y;
        if (headTransform == null)
            headTransform = transform.Find("Head");
        if (visualModelRoot == null)
        {
            // Default to visible third-person mesh root. In this prefab, "Head" is a direct child
            // and is guaranteed to exist on remote players even when weapon clusters differ.
            visualModelRoot = transform.Find("Head");
        }
        if (visualModelRoot == null)
        {
            visualModelRoot = headTransform;
        }
        if (visualModelRoot != null)
        {
            standingVisualLocalPosition = visualModelRoot.localPosition;
            standingVisualLocalScale = visualModelRoot.localScale;
        }
        CalibrateStandingHitboxToHeadTip();

        if (photonView.IsMine)
        {
            SetLocalCrouchProperty(false);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
            SyncRemoteCrouchFromOwnerProperties();

        UpdateCrouchVisualBlend();

        // Only process input for local player
        if (!photonView.IsMine) return;

        TickAbilityCooldowns();
        HandleMouseLook();
        HandleMovement();
    }

    /// <summary>
    /// Handles mouse look for first-person camera rotation.
    /// </summary>
    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Horizontal rotation - rotate the whole player
        transform.Rotate(Vector3.up * mouseX);

        // Vertical rotation - only rotate the camera holder
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    /// <summary>
    /// Handles WASD movement, sprinting, and jumping.
    /// </summary>
    private void HandleMovement()
    {
        if (characterController == null) return;

        // Ground check
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        // Crouch (Left Control) - hold to crouch; sync state to all clients.
        bool wantCrouch = Input.GetKey(KeyCode.LeftControl);
        if (wantCrouch != lastSentCrouchState)
        {
            lastSentCrouchState = wantCrouch;
            ApplyCrouchState(wantCrouch);
            SetLocalCrouchProperty(wantCrouch);
            photonView.RPC(nameof(RPC_SetCrouchState), RpcTarget.Others, wantCrouch);
        }

        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        cachedMoveInput = new Vector2(horizontal, vertical);

        // Calculate movement direction relative to player facing
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;

        // Sprint (only when not crouching) and crouch speed
        float currentSpeed = walkSpeed;
        if (isCrouching)
            currentSpeed = walkSpeed * crouchSpeedMultiplier;
        else if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed = sprintSpeed;

        // Apply movement
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        TryActivateMovementAbilities(moveDirection);
        ApplyWallSkimMotion();

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void TickAbilityCooldowns()
    {
        if (sizzleStepCooldownTimer > 0f)
            sizzleStepCooldownTimer -= Time.deltaTime;
        if (wallSkimCooldownTimer > 0f)
            wallSkimCooldownTimer -= Time.deltaTime;
        if (launchPattyCooldownTimer > 0f)
            launchPattyCooldownTimer -= Time.deltaTime;
        if (wallSkimTimer <= 0f && wallSkimMomentumMultiplier > 1f)
        {
            float decay = Mathf.Max(0f, wallSkimMomentumDecay);
            wallSkimMomentumMultiplier = Mathf.MoveTowards(wallSkimMomentumMultiplier, 1f, decay * Time.deltaTime);
            if (wallSkimMomentumMultiplier <= 1.001f)
                hasLastWallNormal = false;
        }
    }

    private void TryActivateMovementAbilities(Vector3 moveDirection)
    {
        TryActivateSizzleStep(moveDirection);
        TryActivateWallSkim();
        TryActivateLaunchPatty();
    }

    private void TryActivateSizzleStep(Vector3 moveDirection)
    {
        if (!enableSizzleStep || sizzleStepCooldownTimer > 0f) return;
        if (!Input.GetKeyDown(sizzleStepKey)) return;

        Vector3 desiredDirection = moveDirection.sqrMagnitude > 0.01f
            ? moveDirection.normalized
            : transform.forward;

        characterController.Move(desiredDirection * Mathf.Max(0f, sizzleStepDistance));
        sizzleStepCooldownTimer = Mathf.Max(0f, sizzleStepCooldown);
    }

    private void TryActivateWallSkim()
    {
        if (!enableWallSkim || wallSkimCooldownTimer > 0f) return;
        if (!Input.GetKeyDown(wallSkimKey)) return;
        if (isGrounded) return;
        if (!TryGetNearbyWallNormal(out Vector3 wallNormal)) return;

        wallSkimTimer = Mathf.Max(0f, wallSkimDuration);
        wallSkimCooldownTimer = Mathf.Max(0f, wallSkimCooldown);
        wallSkimMomentumMultiplier = Mathf.Max(1f, wallSkimMomentumMultiplier);
        lastWallNormal = wallNormal;
        hasLastWallNormal = true;
    }

    private void ApplyWallSkimMotion()
    {
        if (wallSkimTimer <= 0f) return;
        wallSkimTimer -= Time.deltaTime;
        wallSkimMomentumMultiplier += Mathf.Max(0f, wallSkimMomentumRampPerSecond) * Time.deltaTime;
        wallSkimMomentumMultiplier = Mathf.Clamp(wallSkimMomentumMultiplier, 1f, Mathf.Max(1f, wallSkimMaxMomentumMultiplier));

        if (TryGetNearbyWallNormal(out Vector3 wallNormal))
        {
            if (hasLastWallNormal)
            {
                float sameWallDot = Vector3.Dot(lastWallNormal, wallNormal);
                if (sameWallDot < wallSkimWallSwitchDotThreshold)
                {
                    wallSkimMomentumMultiplier += Mathf.Max(0f, wallSkimComboBoost);
                }
            }

            wallSkimMomentumMultiplier = Mathf.Clamp(wallSkimMomentumMultiplier, 1f, Mathf.Max(1f, wallSkimMaxMomentumMultiplier));
            lastWallNormal = wallNormal;
            hasLastWallNormal = true;

            Vector3 desiredForward = transform.forward;
            if (cachedMoveInput.sqrMagnitude > 0.01f)
            {
                desiredForward = (transform.right * cachedMoveInput.x + transform.forward * cachedMoveInput.y).normalized;
            }

            Vector3 alongWall = Vector3.ProjectOnPlane(desiredForward, wallNormal).normalized;
            if (alongWall.sqrMagnitude > 0.001f)
            {
                characterController.Move(alongWall * walkSpeed * wallSkimMomentumMultiplier * Time.deltaTime);
            }
        }

        characterController.Move(Vector3.up * Mathf.Max(0f, wallSkimRiseSpeed) * Time.deltaTime);
        velocity.y = Mathf.Max(velocity.y, wallSkimFallSpeed);
    }

    private void TryActivateLaunchPatty()
    {
        if (!enableLaunchPatty || launchPattyCooldownTimer > 0f) return;
        if (!Input.GetKeyDown(launchPattyKey)) return;
        if (!isGrounded) return;

        float safeHeight = Mathf.Max(0f, launchPattyHeight);
        if (safeHeight > 0f)
        {
            velocity.y = Mathf.Sqrt(safeHeight * -2f * gravity);
        }

        Vector3 forwardBurst = transform.forward * Mathf.Max(0f, launchPattyForwardBoost) * Time.deltaTime;
        characterController.Move(forwardBurst);
        launchPattyCooldownTimer = Mathf.Max(0f, launchPattyCooldown);
    }

    private bool TryGetNearbyWallNormal(out Vector3 wallNormal)
    {
        Vector3 origin = transform.position + Vector3.up * (characterController != null ? characterController.height * 0.45f : 0.9f);
        Vector3[] directions =
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right
        };

        float detectDistance = Mathf.Max(0.1f, wallDetectDistance);
        for (int i = 0; i < directions.Length; i++)
        {
            if (Physics.Raycast(origin, directions[i], out RaycastHit hit, detectDistance, wallSkimLayers, QueryTriggerInteraction.Ignore))
            {
                wallNormal = hit.normal;
                return true;
            }
        }

        wallNormal = Vector3.up;
        return false;
    }

    private void OnGUI()
    {
        if (!showAbilityDebugHud || !photonView.IsMine) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 14,
            richText = true
        };

        string sizzleState = enableSizzleStep
            ? FormatCooldownState("Sizzle Step", sizzleStepCooldownTimer)
            : "<color=#999999>Sizzle Step: OFF</color>";
        string wallSkimState = enableWallSkim
            ? $"{FormatCooldownState("Wall Skim", wallSkimCooldownTimer)}\nSkim Time Left: {Mathf.Max(0f, wallSkimTimer):0.00}s | Momentum: {wallSkimMomentumMultiplier:0.00}x"
            : "<color=#999999>Wall Skim: OFF</color>";
        string launchPattyState = enableLaunchPatty
            ? FormatCooldownState("Launch Patty", launchPattyCooldownTimer)
            : "<color=#999999>Launch Patty: OFF</color>";

        string hudText =
            "<b>Ability Debug</b>\n" +
            $"{sizzleState}\n" +
            $"{wallSkimState}\n" +
            $"{launchPattyState}";

        Rect panel = new Rect(
            Mathf.Max(0f, abilityDebugHudScreenOffset.x),
            Mathf.Max(0f, abilityDebugHudScreenOffset.y),
            360f,
            125f);
        GUI.Box(panel, hudText, style);
    }

    private static string FormatCooldownState(string abilityName, float cooldownTimer)
    {
        if (cooldownTimer <= 0f)
            return $"<color=#6CFF8C>{abilityName}: READY</color>";
        return $"<color=#FFD36C>{abilityName}: {cooldownTimer:0.00}s</color>";
    }

    public void SetAbilityDebugHudScreenOffset(Vector2 newOffset)
    {
        abilityDebugHudScreenOffset = newOffset;
    }

    /// <summary>
    /// Call this to temporarily disable player input (e.g., when dead).
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    /// <summary>
    /// Returns true only when a world hit point is inside (or very near) the calibrated damage hitbox.
    /// </summary>
    public bool IsPointInsideDamageHitbox(Vector3 worldPoint, float tolerance = 0.03f)
    {
        if (hitboxCollider == null) return true;
        Vector3 closest = hitboxCollider.ClosestPoint(worldPoint);
        return (closest - worldPoint).sqrMagnitude <= (tolerance * tolerance);
    }

    /// <summary>
    /// Universal weapon hit check: direct head collider hits are valid, otherwise point must be inside calibrated damage hitbox.
    /// </summary>
    public bool IsValidDamageHit(Collider hitCollider, Vector3 worldPoint, float tolerance = 0.03f)
    {
        if (hitCollider == null) return false;
        if (headTransform != null && hitCollider.transform.IsChildOf(headTransform))
            return true;
        return IsPointInsideDamageHitbox(worldPoint, tolerance);
    }

    [PunRPC]
    private void RPC_SetCrouchState(bool crouching)
    {
        ApplyCrouchState(crouching);
    }

    private void ApplyCrouchState(bool crouching)
    {
        if (characterController == null) return;
        if (standingHeight <= 0f)
        {
            standingHeight = characterController.height;
            standingCenter = characterController.center;
        }
        if (hitboxCollider != null && standingHitboxHeight <= 0f)
        {
            standingHitboxHeight = hitboxCollider.height;
            standingHitboxCenter = hitboxCollider.center;
        }

        float targetHeight = crouching ? standingHeight * Mathf.Clamp01(crouchHeightPercent) : standingHeight;
        float targetCenterY = standingCenter.y - (standingHeight - targetHeight) * 0.5f;

        characterController.enabled = false;
        characterController.height = targetHeight;
        characterController.center = new Vector3(standingCenter.x, targetCenterY, standingCenter.z);
        characterController.enabled = true;

        if (hitboxCollider != null && standingHitboxHeight > 0f)
        {
            float scale = crouching ? crouchHitboxScale : standingHitboxScale;
            float targetHitboxHeight = standingHitboxHeight * Mathf.Max(0.2f, scale);
            float targetHitboxCenterY = standingHitboxCenter.y - (standingHitboxHeight - targetHitboxHeight) * 0.5f;
            hitboxCollider.height = targetHitboxHeight;
            hitboxCollider.center = new Vector3(standingHitboxCenter.x, targetHitboxCenterY, standingHitboxCenter.z);
        }

        isCrouching = crouching;
        crouchBlendTarget = crouching ? 1f : 0f;
    }

    private void UpdateCrouchVisualBlend()
    {
        float safeTransitionSpeed = Mathf.Max(0.01f, crouchTransitionSpeed);
        crouchBlend = Mathf.MoveTowards(crouchBlend, crouchBlendTarget, Time.deltaTime * safeTransitionSpeed);

        if (cameraHolder != null && photonView.IsMine && standingHeight > 0f)
        {
            float crouchHeight = standingHeight * Mathf.Clamp01(crouchHeightPercent);
            float cameraDrop = standingHeight - crouchHeight;
            Vector3 p = cameraHolder.localPosition;
            cameraHolder.localPosition = new Vector3(p.x, standingCameraY - (cameraDrop * crouchBlend), p.z);
        }

        if (visualModelRoot != null)
        {
            float modelScaleY = Mathf.Lerp(1f, Mathf.Max(0.4f, crouchVisualScaleY), crouchBlend);
            float modelDrop = Mathf.Lerp(0f, Mathf.Max(0f, crouchVisualDrop), crouchBlend);

            visualModelRoot.localScale = new Vector3(
                standingVisualLocalScale.x,
                standingVisualLocalScale.y * modelScaleY,
                standingVisualLocalScale.z);

            visualModelRoot.localPosition = standingVisualLocalPosition + (Vector3.down * modelDrop);
        }
    }

    private void SyncRemoteCrouchFromOwnerProperties()
    {
        if (photonView.Owner == null) return;
        if (!photonView.Owner.CustomProperties.TryGetValue(CrouchPropertyKey, out object rawValue)) return;
        if (!(rawValue is bool crouchState)) return;
        if (crouchState == isCrouching) return;

        ApplyCrouchState(crouchState);
    }

    private void SetLocalCrouchProperty(bool crouchState)
    {
        Hashtable props = new Hashtable
        {
            { CrouchPropertyKey, crouchState }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private void CalibrateStandingHitboxToHeadTip()
    {
        if (hitboxCollider == null || characterController == null) return;

        float feetY = standingCenter.y - (standingHeight * 0.5f);
        float headTopY = GetHeadTopLocalY();

        // Hard-cap top near the visible head to avoid oversized vertical hitbox.
        if (headTransform != null)
        {
            float strictHeadTop = headTransform.localPosition.y + Mathf.Max(0.02f, standingHeadTopPadding);
            headTopY = Mathf.Min(headTopY, strictHeadTop);
        }

        // Apply an additional global shrink for a noticeably tighter standing hitbox.
        float rawHeight = headTopY - feetY;
        float shrunkenHeight = rawHeight * Mathf.Clamp(standingHitboxScale, 0.25f, 1f);
        headTopY = feetY + shrunkenHeight;
        if (headTopY <= feetY + 0.05f) return;

        standingHitboxHeight = headTopY - feetY;
        standingHitboxCenter = new Vector3(
            standingHitboxCenter.x,
            feetY + (standingHitboxHeight * 0.5f),
            standingHitboxCenter.z);

        hitboxCollider.direction = 1; // Y axis
        hitboxCollider.height = standingHitboxHeight;
        hitboxCollider.center = standingHitboxCenter;
    }

    private float GetHeadTopLocalY()
    {
        if (headTransform != null)
        {
            Renderer[] headRenderers = headTransform.GetComponentsInChildren<Renderer>(true);
            if (headRenderers != null && headRenderers.Length > 0)
            {
                float maxY = float.MinValue;
                foreach (Renderer r in headRenderers)
                {
                    Bounds b = r.bounds;
                    Vector3 ext = b.extents;
                    Vector3 c = b.center;
                    Vector3[] corners =
                    {
                        new Vector3(c.x - ext.x, c.y - ext.y, c.z - ext.z),
                        new Vector3(c.x - ext.x, c.y - ext.y, c.z + ext.z),
                        new Vector3(c.x - ext.x, c.y + ext.y, c.z - ext.z),
                        new Vector3(c.x - ext.x, c.y + ext.y, c.z + ext.z),
                        new Vector3(c.x + ext.x, c.y - ext.y, c.z - ext.z),
                        new Vector3(c.x + ext.x, c.y - ext.y, c.z + ext.z),
                        new Vector3(c.x + ext.x, c.y + ext.y, c.z - ext.z),
                        new Vector3(c.x + ext.x, c.y + ext.y, c.z + ext.z)
                    };
                    for (int i = 0; i < corners.Length; i++)
                    {
                        float localY = transform.InverseTransformPoint(corners[i]).y;
                        if (localY > maxY) maxY = localY;
                    }
                }
                if (maxY > float.MinValue) return maxY;
            }
            return headTransform.localPosition.y + 0.5f * Mathf.Abs(headTransform.localScale.y);
        }

        return standingCenter.y + (standingHeight * 0.5f);
    }
}
