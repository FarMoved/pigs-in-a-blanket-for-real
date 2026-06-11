using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Local-client silhouette outlines on other players (enemies / optional teammates).
/// </summary>
[DisallowMultipleComponent]
public class PlayerOutlineHighlighter : MonoBehaviour
{
    private struct OutlineEntry
    {
        public Renderer Source;
        public Renderer Outline;
    }

    private static Material sharedOutlineMaterial;
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly List<OutlineEntry> entries = new List<OutlineEntry>();
    private PhotonView photonView;
    private PlayerHealth playerHealth;
    private MaterialPropertyBlock propertyBlock;
    private bool built;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        playerHealth = GetComponent<PlayerHealth>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        EnsureBuilt();
        ApplySettings();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Outline != null)
                Destroy(entries[i].Outline.gameObject);
        }
        entries.Clear();
    }

    public static void RefreshAll()
    {
        foreach (PlayerOutlineHighlighter highlighter in FindObjectsOfType<PlayerOutlineHighlighter>())
            highlighter.ApplySettings();
    }

    public void ApplySettings()
    {
        EnsureBuilt();

        if (photonView == null || photonView.IsMine)
        {
            SetVisible(false);
            return;
        }

        if (playerHealth != null && playerHealth.IsDead)
        {
            SetVisible(false);
            return;
        }

        Team localTeam = TeamManager.GetLocalPlayerTeam();
        Team playerTeam = photonView.Owner != null
            ? TeamManager.GetPlayerTeam(photonView.Owner)
            : Team.None;

        bool isEnemy = localTeam != Team.None && playerTeam != Team.None && localTeam != playerTeam;
        bool isTeammate = localTeam != Team.None && playerTeam == localTeam;

        bool show = (isEnemy && GameSettings.EnemyOutlinesEnabled)
                    || (isTeammate && GameSettings.TeammateOutlinesEnabled);
        if (!show)
        {
            SetVisible(false);
            return;
        }

        Color color = isEnemy
            ? GameSettings.GetEnemyOutlineColor()
            : GameSettings.GetTeammateOutlineColor();

        propertyBlock.SetColor(ColorId, color);
        for (int i = 0; i < entries.Count; i++)
        {
            Renderer outline = entries[i].Outline;
            if (outline == null) continue;
            outline.enabled = true;
            outline.SetPropertyBlock(propertyBlock);
        }
    }

    private void EnsureBuilt()
    {
        if (built) return;
        built = true;

        Material material = GetSharedMaterial();
        if (material == null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer source = renderers[i];
            if (source == null || !ShouldOutlineRenderer(source))
                continue;

            Renderer outline = CreateOutlineRenderer(source, material);
            if (outline == null) continue;

            entries.Add(new OutlineEntry { Source = source, Outline = outline });
        }
    }

    private static bool ShouldOutlineRenderer(Renderer renderer)
    {
        if (renderer is ParticleSystemRenderer)
            return false;
        if (renderer.GetComponent<Camera>() != null)
            return false;
        if (!renderer.gameObject.activeInHierarchy && !renderer.enabled)
            return false;

        Transform t = renderer.transform;
        while (t != null)
        {
            string name = t.name;
            if (name == "Camera" || name == "CameraHolder")
                return false;
            t = t.parent;
        }

        return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
    }

    private static Renderer CreateOutlineRenderer(Renderer source, Material material)
    {
        GameObject outlineGo = new GameObject(source.name + "_Outline");
        outlineGo.transform.SetParent(source.transform, false);
        outlineGo.layer = source.gameObject.layer;

        Renderer outlineRenderer;
        if (source is SkinnedMeshRenderer skinned)
        {
            var outlineSkinned = outlineGo.AddComponent<SkinnedMeshRenderer>();
            outlineSkinned.sharedMesh = skinned.sharedMesh;
            outlineSkinned.bones = skinned.bones;
            outlineSkinned.rootBone = skinned.rootBone;
            outlineSkinned.quality = skinned.quality;
            outlineSkinned.updateWhenOffscreen = true;
            outlineRenderer = outlineSkinned;
        }
        else
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                return null;

            outlineGo.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
            outlineRenderer = outlineGo.AddComponent<MeshRenderer>();
        }

        outlineRenderer.sharedMaterial = material;
        outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.enabled = false;
        return outlineRenderer;
    }

    private void SetVisible(bool visible)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Outline != null)
                entries[i].Outline.enabled = visible;
        }
    }

    private static Material GetSharedMaterial()
    {
        if (sharedOutlineMaterial != null)
            return sharedOutlineMaterial;

        Shader shader = Shader.Find("PigsInABlanket/PlayerOutline");
        if (shader == null)
        {
            Debug.LogWarning("PlayerOutlineHighlighter: shader PigsInABlanket/PlayerOutline not found.");
            return null;
        }

        sharedOutlineMaterial = new Material(shader);
        return sharedOutlineMaterial;
    }
}
