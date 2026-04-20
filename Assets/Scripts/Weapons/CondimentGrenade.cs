using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// The grenade projectile for the Condiment Cluster weapon.
/// Explodes after a fuse time, creating cluster explosions.
/// </summary>
public class CondimentGrenade : MonoBehaviourPunCallbacks
{
    [Header("Grenade Settings")]
    [SerializeField] private float bounciness = 0.5f;

    [Header("Effects")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private GameObject clusterEffectPrefab;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip bounceSound;

    private Rigidbody rb;
    private int damage;
    private float explosionRadius;
    private float fuseTime;
    private int clusterCount;
    private int ownerViewID;
    private int ownerActorNumber;
    private bool hasExploded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }

    /// <summary>
    /// Initialize the grenade with throw parameters.
    /// </summary>
    public void Initialize(Vector3 velocity, int damage, float explosionRadius, float fuseTime, int clusterCount, int ownerViewID, int ownerActorNumber)
    {
        this.damage = damage;
        this.explosionRadius = explosionRadius;
        this.fuseTime = fuseTime;
        this.clusterCount = clusterCount;
        this.ownerViewID = ownerViewID;
        this.ownerActorNumber = ownerActorNumber;

        // Apply throw velocity
        if (rb != null)
        {
            rb.velocity = velocity;
            rb.angularVelocity = Random.insideUnitSphere * 5f;
        }

        // Start fuse countdown
        StartCoroutine(FuseCountdown());
    }

    private IEnumerator FuseCountdown()
    {
        yield return new WaitForSeconds(fuseTime);

        if (!hasExploded)
        {
            Explode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Play bounce sound
        if (bounceSound != null && collision.relativeVelocity.magnitude > 1f)
        {
            AudioSource.PlayClipAtPoint(bounceSound, transform.position, 0.5f);
        }

        // Apply bounciness
        if (rb != null)
        {
            rb.velocity *= bounciness;
        }
    }

    /// <summary>
    /// Main explosion logic.
    /// </summary>
    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Vector3 explosionPos = transform.position;

        // Only the owner calculates damage
        if (photonView.IsMine)
        {
            // Main explosion damage
            ApplyExplosionDamage(explosionPos, damage, explosionRadius);

            // Create cluster explosions
            for (int i = 0; i < clusterCount; i++)
            {
                // Random offset for cluster position
                Vector3 clusterOffset = Random.insideUnitSphere * explosionRadius * 0.5f;
                clusterOffset.y = Mathf.Abs(clusterOffset.y); // Keep clusters above ground
                Vector3 clusterPos = explosionPos + clusterOffset;

                // Delayed cluster explosion
                StartCoroutine(ClusterExplosion(clusterPos, i * 0.1f));
            }
        }

        // Sync explosion effect to all players
        photonView.RPC("RPC_Explode", RpcTarget.All, explosionPos);

        // Destroy after clusters complete
        if (photonView.IsMine)
        {
            StartCoroutine(DestroyAfterClusters());
        }
    }

    private IEnumerator ClusterExplosion(Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Apply cluster damage (reduced from main explosion)
        ApplyExplosionDamage(position, damage / 2, explosionRadius * 0.5f);

        // Show cluster effect
        photonView.RPC("RPC_ClusterExplosion", RpcTarget.All, position);
    }

    /// <summary>
    /// Applies explosion damage to all players in radius.
    /// </summary>
    private void ApplyExplosionDamage(Vector3 center, int maxDamage, float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(center, radius);
        System.Collections.Generic.HashSet<int> damagedViews = new System.Collections.Generic.HashSet<int>();

        foreach (Collider col in hitColliders)
        {
            PlayerHealth targetHealth = col.GetComponentInParent<PlayerHealth>();
            if (targetHealth != null &&
                targetHealth.photonView.ViewID != ownerViewID &&
                !damagedViews.Contains(targetHealth.photonView.ViewID))
            {
                PlayerController targetController = targetHealth.GetComponent<PlayerController>();
                Vector3 samplePoint = col.ClosestPoint(center);
                bool validHit = targetController == null || targetController.IsValidDamageHit(col, samplePoint, 0.08f);
                if (!validHit) continue;

                // Don't damage the thrower
                float distance = Vector3.Distance(center, col.transform.position);
                float damageMultiplier = 1f - (distance / radius);
                damageMultiplier = Mathf.Clamp01(damageMultiplier);
                int finalDamage = Mathf.RoundToInt(maxDamage * damageMultiplier);

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

    private IEnumerator DestroyAfterClusters()
    {
        yield return new WaitForSeconds(clusterCount * 0.1f + 0.5f);
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    private void RPC_Explode(Vector3 position)
    {
        // Spawn main explosion effect
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, position, Quaternion.identity);
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, position);
        }

        // Hide grenade mesh
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.enabled = false;
        }
    }

    [PunRPC]
    private void RPC_ClusterExplosion(Vector3 position)
    {
        // Spawn cluster effect
        if (clusterEffectPrefab != null)
        {
            GameObject effect = Instantiate(clusterEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
