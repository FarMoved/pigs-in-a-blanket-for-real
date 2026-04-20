using UnityEngine;
using Photon.Pun;

/// <summary>
/// Hides the local player's head/body (e.g. nose, head) from the local camera only,
/// so it doesn't block the view. Other players still see your full model.
/// </summary>
public class HideLocalPlayerBody : MonoBehaviour
{
    [Tooltip("Transforms to hide (e.g. Head). All Renderers on these and their children are disabled for the local player. If empty, finds 'Head' by name.")]
    [SerializeField] private Transform[] bodyPartsToHide;

    private void Start()
    {
        if (!GetComponent<PhotonView>().IsMine) return;

        if (bodyPartsToHide != null && bodyPartsToHide.Length > 0)
        {
            foreach (Transform t in bodyPartsToHide)
            {
                if (t != null)
                    SetRenderersEnabled(t, false);
            }
        }
        else
        {
            Transform head = transform.Find("Head");
            if (head != null)
                SetRenderersEnabled(head, false);
        }
    }

    private static void SetRenderersEnabled(Transform root, bool enabled)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            r.enabled = enabled;
    }
}
