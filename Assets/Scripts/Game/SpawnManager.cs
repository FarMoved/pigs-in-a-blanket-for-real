using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages spawn points for each team.
/// The map is divided into 25% Red, 50% Neutral, 25% Blue zones.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Point Configuration")]
    [SerializeField] private Transform[] redSpawnPoints;
    [SerializeField] private Transform[] blueSpawnPoints;

    [Header("Map Bounds (for procedural spawning)")]
    [SerializeField] private Vector3 mapCenter = Vector3.zero;
    [SerializeField] private Vector3 mapSize = new Vector3(50f, 0f, 50f);
    [SerializeField] private float spawnHeight = 1f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    /// <summary>
    /// Gets a spawn point for the specified team.
    /// </summary>
    public Vector3 GetSpawnPoint(Team team)
    {
        Transform[] spawnPoints = team == Team.Red ? redSpawnPoints : blueSpawnPoints;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Return random spawn point from array
            int index = Random.Range(0, spawnPoints.Length);
            return spawnPoints[index].position;
        }

        // Fallback: Generate spawn point procedurally
        return GenerateProceduralSpawnPoint(team);
    }

    /// <summary>
    /// Generates a spawn point procedurally based on team zones.
    /// Map layout: [Red 25%][Neutral 50%][Blue 25%]
    /// </summary>
    private Vector3 GenerateProceduralSpawnPoint(Team team)
    {
        float halfWidth = mapSize.x / 2f;
        float halfDepth = mapSize.z / 2f;

        float minX, maxX;

        if (team == Team.Red)
        {
            // Red team: Left 25% of map
            minX = mapCenter.x - halfWidth;
            maxX = mapCenter.x - halfWidth + (mapSize.x * 0.25f);
        }
        else
        {
            // Blue team: Right 25% of map
            minX = mapCenter.x + halfWidth - (mapSize.x * 0.25f);
            maxX = mapCenter.x + halfWidth;
        }

        // Random position within team zone
        float x = Random.Range(minX, maxX);
        float z = Random.Range(mapCenter.z - halfDepth, mapCenter.z + halfDepth);

        // Try to find valid ground position
        Vector3 spawnPos = new Vector3(x, mapCenter.y + 10f, z);

        RaycastHit hit;
        if (Physics.Raycast(spawnPos, Vector3.down, out hit, 20f))
        {
            spawnPos = hit.point + Vector3.up * spawnHeight;
        }
        else
        {
            spawnPos = new Vector3(x, mapCenter.y + spawnHeight, z);
        }

        return spawnPos;
    }

    /// <summary>
    /// Gets the center of a team's spawn zone.
    /// </summary>
    public Vector3 GetTeamZoneCenter(Team team)
    {
        float halfWidth = mapSize.x / 2f;

        if (team == Team.Red)
        {
            return mapCenter + Vector3.left * (halfWidth - mapSize.x * 0.125f);
        }
        else if (team == Team.Blue)
        {
            return mapCenter + Vector3.right * (halfWidth - mapSize.x * 0.125f);
        }

        return mapCenter;
    }

    /// <summary>
    /// Checks if a position is in a team's territory.
    /// </summary>
    public Team GetTerritory(Vector3 position)
    {
        float halfWidth = mapSize.x / 2f;
        float relativeX = position.x - mapCenter.x;

        // Normalize to -0.5 to 0.5 range
        float normalizedX = relativeX / mapSize.x;

        if (normalizedX < -0.25f)
        {
            return Team.Red; // Left 25%
        }
        else if (normalizedX > 0.25f)
        {
            return Team.Blue; // Right 25%
        }

        return Team.None; // Neutral zone (middle 50%)
    }

    /// <summary>
    /// Sets map bounds at runtime.
    /// </summary>
    public void SetMapBounds(Vector3 center, Vector3 size)
    {
        mapCenter = center;
        mapSize = size;
    }

    /// <summary>
    /// Adds spawn points at runtime.
    /// </summary>
    public void SetSpawnPoints(Transform[] red, Transform[] blue)
    {
        redSpawnPoints = red;
        blueSpawnPoints = blue;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        float halfWidth = mapSize.x / 2f;
        float halfDepth = mapSize.z / 2f;
        float height = 5f;

        // Red zone (left 25%)
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Vector3 redCenter = mapCenter + Vector3.left * (halfWidth - mapSize.x * 0.125f);
        Vector3 redSize = new Vector3(mapSize.x * 0.25f, height, mapSize.z);
        Gizmos.DrawCube(redCenter, redSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(redCenter, redSize);

        // Blue zone (right 25%)
        Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
        Vector3 blueCenter = mapCenter + Vector3.right * (halfWidth - mapSize.x * 0.125f);
        Vector3 blueSize = new Vector3(mapSize.x * 0.25f, height, mapSize.z);
        Gizmos.DrawCube(blueCenter, blueSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(blueCenter, blueSize);

        // Neutral zone (middle 50%)
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        Vector3 neutralSize = new Vector3(mapSize.x * 0.5f, height, mapSize.z);
        Gizmos.DrawCube(mapCenter, neutralSize);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(mapCenter, neutralSize);

        // Draw spawn points
        if (redSpawnPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform spawn in redSpawnPoints)
            {
                if (spawn != null)
                {
                    Gizmos.DrawSphere(spawn.position, 0.5f);
                }
            }
        }

        if (blueSpawnPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform spawn in blueSpawnPoints)
            {
                if (spawn != null)
                {
                    Gizmos.DrawSphere(spawn.position, 0.5f);
                }
            }
        }
    }
}
