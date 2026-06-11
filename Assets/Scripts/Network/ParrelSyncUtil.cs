using System.IO;
using UnityEngine;

/// <summary>
/// Detects ParrelSync clone editors without referencing the ParrelSync editor assembly.
/// </summary>
public static class ParrelSyncUtil
{
    public static bool IsClone()
    {
#if UNITY_EDITOR
        string markerPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".clone"));
        return File.Exists(markerPath);
#else
        return false;
#endif
    }
}
