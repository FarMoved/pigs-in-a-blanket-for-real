# Getting Actual Models and Art

The game uses placeholder shapes (capsules, cubes, spheres) for weapons and the player. Use this guide to find and attach real 3D models.

---

## Where to get models

- **Unity Asset Store** – In Unity: **Window → Asset Store**. Search "free 3D", "weapon", "character", or "food". Check each asset’s license (e.g. free for personal/commercial). Import via the Package Manager or Asset Store window.
- **Sketchfab** – [sketchfab.com](https://sketchfab.com). Filter by **Downloadable** and license (e.g. CC BY, CC0). Export as FBX and import into Unity (**Assets → Import New Asset**).
- **OpenGameArt / itch.io** – Free game art; filter by 3D and license. Import FBX or OBJ into Unity.
- **Mixamo** – Good for **character** rigs and animations (humanoid). Less useful for props like weapons or food.
- Prefer one **low-poly** or **stylized** pack so the art style stays consistent.

---

## Where to attach models in this project

| Object | Prefab / Location | What to replace / add |
|--------|-------------------|------------------------|
| **Player** | **PigPlayer** (Assets/Resources or Photon Resources) | Character mesh: assign to **Player Model** on PlayerHealth (or the visible body under the player root). Use a pig or stylized character if you have one. |
| **Hot Dog Launcher** | Child of PigPlayer (under CameraHolder) | Replace the placeholder **Capsule** child with your weapon mesh, or assign a mesh to a child and scale/position. Optional: set **Muzzle Point** in HotDogLauncher script. |
| **Ketchup Pistol** | Child of PigPlayer (under CameraHolder) | Replace the placeholder **Cube** child with a pistol/bottle mesh. |
| **Spatula Slapper** | Child of PigPlayer (under CameraHolder) | Assign a **Spatula Model** transform (child with the spatula mesh) in the SpatulaSlapper component so the swing animation rotates it. Optionally replace the placeholder cube. |
| **Condiment Cluster** | Child of PigPlayer (under CameraHolder) | Replace the placeholder **Sphere** with a grenade/bottle mesh. |

---

## Import and setup in Unity

1. Put FBX/OBJ (or Asset Store imports) in e.g. **Assets/Models/**.
2. Select the model in the Project window; in the Inspector adjust **Scale**, **Mesh Compression**, and **Materials** as needed.
3. For **weapons**: open the **PigPlayer** prefab, locate the weapon (e.g. HotDogLauncher). Add a child GameObject with a **MeshFilter** and **MeshRenderer** (or drag your prefab/model as a child), assign the mesh/material, then position/scale so it sits in front of the camera. Assign any required reference (e.g. Spatula Model, Muzzle Point) in the weapon script.
4. For the **player body**: use a character model as the visible body; keep the first-person camera and weapon hierarchy as-is so controls and networking are unchanged.

No code changes are required; this is asset import and prefab/scene setup.
