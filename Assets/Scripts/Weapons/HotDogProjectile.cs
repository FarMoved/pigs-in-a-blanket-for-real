using UnityEngine;
using Photon.Pun;

/// <summary>
/// Projectile spawned by the Hot Dog Launcher.
/// Arcs through the air and explodes on impact.
/// </summary>
public class HotDogProjectile : MonoBehaviourPunCallbacks
{
    [Header("Projectile Settings")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float lifetime = 5f;

    [Header("Effects")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private AudioClip explosionSound;

    private float speed;
    private int damage;
    private float explosionRadius;
    private int ownerViewID;
    private int ownerActorNumber;
    private Vector3 velocity;
    private bool hasExploded = false;

    private bool IsOwnerCollider(Collider col)
    {
        PhotonView ownerView = PhotonView.Find(ownerViewID);
        if (ownerView == null || col == null) return false;
        PhotonView hitView = col.GetComponentInParent<PhotonView>();
        return hitView == ownerView;
    }

    /// <summary>
    /// Initialize the projectile with launch parameters.
    /// </summary>
    public void Initialize(float speed, int damage, float explosionRadius, int ownerViewID, int ownerActorNumber)
    {
        this.speed = speed;
        this.damage = damage;
        this.explosionRadius = explosionRadius;
        this.ownerViewID = ownerViewID;
        this.ownerActorNumber = ownerActorNumber;

        // Set initial velocity in forward direction
        velocity = transform.forward * speed;

        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (hasExploded) return;

        // Apply gravity for arc effect
        velocity.y += gravity * Time.deltaTime;

        // Move projectile
        Vector3 movement = velocity * Time.deltaTime;

        // Check for collision along path
        RaycastHit hit;
        if (Physics.Raycast(transform.position, movement.normalized, out hit, movement.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (IsOwnerCollider(hit.collider))
            {
                transform.position += movement;
                return;
            }
            Explode(hit.point);
            return;
        }

        // Update position and rotation
        transform.position += movement;

        // Rotate to face velocity direction
        if (velocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasExploded)
        {
            Explode(collision.contacts[0].point);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasExploded)
        {
            if (other == null || other.isTrigger) return;
            if (IsOwnerCollider(other)) return;
            Explode(transform.position);
        }
    }

    /// <summary>
    /// Handles explosion logic - damage and effects.
    /// </summary>
    private void Explode(Vector3 position)
    {
        if (hasExploded) return;
        hasExploded = true;

        // Only the owner calculates damage
        if (photonView.IsMine)
        {
            // Find all players in explosion radius
            Collider[] hitColliders = Physics.OverlapSphere(position, explosionRadius);
            System.Collections.Generic.HashSet<int> damagedViews = new System.Collections.Generic.HashSet<int>();
            foreach (Collider col in hitColliders)
            {
                PlayerHealth targetHealth = col.GetComponentInParent<PlayerHealth>();
                if (targetHealth != null &&
                    targetHealth.photonView.ViewID != ownerViewID &&
                    !damagedViews.Contains(targetHealth.photonView.ViewID))
                {
                    PlayerController targetController = targetHealth.GetComponent<PlayerController>();
                    Vector3 samplePoint = col.ClosestPoint(position);
                    bool validHit = targetController == null || targetController.IsValidDamageHit(col, samplePoint, 0.08f);
                    if (!validHit) continue;

                    // Don't damage the shooter
                    float distance = Vector3.Distance(position, col.transform.position);
                    float damageMultiplier = 1f - (distance / explosionRadius);
                    damageMultiplier = Mathf.Clamp01(damageMultiplier);
                    int finalDamage = Mathf.RoundToInt(damage * damageMultiplier);

                    if (finalDamage > 0)
                    {
                        targetHealth.TakeDamage(finalDamage, ownerViewID, ownerActorNumber);
                        damagedViews.Add(targetHealth.photonView.ViewID);
                        if (photonView.IsMine)
                        {
                            var hud = FindObjectOfType<HUDController>();
                            if (hud != null) hud.ShowHitIndicator();
                        }
                }
            }
        }
        }

        // Spawn explosion effect (visible to all)
        photonView.RPC("RPC_Explode", RpcTarget.All, position);
    }

    [PunRPC]
    private void RPC_Explode(Vector3 position)
    {
        // Spawn explosion effect
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, position, Quaternion.identity);
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, position);
        }

        // Destroy the projectile
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    /// <summary>
    /// Draw explosion radius in editor for debugging.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
