# How to Add Holdable Weapons to PigPlayer

Follow these steps in Unity so you can use weapons after pressing Connect and spawning.

---

## 1. Open the PigPlayer Prefab

- In **Project** go to **Assets > Resources**
- **Double-click** **PigPlayer** to open Prefab mode

---

## 2. Add WeaponManager (if not already there)

- Select the root **PigPlayer** in the Hierarchy
- In the Inspector click **Add Component**
- Search for **Weapon Manager** and add it

---

## 3. Create the Four Weapon Objects

Create **four empty child objects** under PigPlayer, then add the weapon scripts.

### Primary – Hot Dog Launcher

1. Right-click **PigPlayer** > **Create Empty**
2. Rename to **HotDogLauncher**
3. Set **Transform**: Position (0.15, -0.25, 0.5), Rotation (0, 0, 0), **Scale (0.12, 0.12, 0.12)** so it doesn’t fill the screen.
4. **Add Component** > search **Hot Dog Launcher** > add it
5. (Optional) Add a **Create Empty** child under HotDogLauncher, name it **MuzzlePoint**, set Position (0, 0, 0.5). In Hot Dog Launcher script, assign **Muzzle Point** to this transform.
6. If you added a **Capsule** as the visual, set that child’s Scale to about (1, 1, 1) so the parent scale above controls size.

### Secondary – Ketchup Pistol

1. Right-click **PigPlayer** > **Create Empty**
2. Rename to **KetchupPistol**
3. Set **Transform**: Position (0, 0.3, 0.5), Rotation (0, 0, 0), Scale (0.5, 0.5, 0.5)
4. **Add Component** > **Ketchup Pistol**

### Melee – Spatula Slapper

1. Right-click **PigPlayer** > **Create Empty**
2. Rename to **SpatulaSlapper**
3. Set **Transform**: Position (0, 0.2, 0.4), Rotation (0, 0, 0), Scale (0.5, 0.5, 0.5)
4. **Add Component** > **Spatula Slapper**
5. (Optional) Add a **3D Object > Cube** as child, scale it flat (e.g. 0.2, 1, 0.5) as the spatula blade. Drag it into the **Spatula Model** field.

### Throwable – Condiment Cluster

1. Right-click **PigPlayer** > **Create Empty**
2. Rename to **CondimentCluster**
3. Set **Transform**: Position (0, 0.3, 0.5), Rotation (0, 0, 0), Scale (0.5, 0.5, 0.5)
4. **Add Component** > **Condiment Cluster**

---

## 4. Assign Weapons to WeaponManager

- Select **PigPlayer** (root)
- In the **Weapon Manager** component you’ll see:
  - **Primary Weapon**
  - **Secondary Weapon**
  - **Melee Weapon**
  - **Throwable Weapon**
- Drag from the Hierarchy into the Inspector:
  - **HotDogLauncher** → Primary Weapon  
  - **KetchupPistol** → Secondary Weapon  
  - **SpatulaSlapper** → Melee Weapon  
  - **CondimentCluster** → Throwable Weapon

---

## 5. Add visible shapes to each weapon (so you can see them in-game)

Weapon objects are empty by default. Add a simple 3D shape as a **child** of each weapon so they’re visible:

- **HotDogLauncher:** Right-click HotDogLauncher > 3D Object > **Capsule**. Scale (0.3, 0.5, 0.3). Position (0, 0, 0.3). Optional: use a material (e.g. brown) for a “bun” look.
- **KetchupPistol:** Right-click KetchupPistol > 3D Object > **Cube**. Scale (0.15, 0.2, 0.4). Position (0, 0, 0.2). Optional: red material for “ketchup bottle”.
- **SpatulaSlapper:** Right-click SpatulaSlapper > 3D Object > **Cube**. Scale (0.1, 0.4, 0.3). Position (0, 0, 0.25). In Spatula Slapper script, drag this cube into **Spatula Model**.
- **CondimentCluster:** Right-click CondimentCluster > 3D Object > **Sphere**. Scale (0.2, 0.2, 0.2). Position (0, 0, 0.2). Optional: red/yellow material.

Adjust position/scale so the weapon sits in front of the camera and doesn’t block the view.

## 6. Parent Weapons Under CameraHolder (so they move with the camera)

So the weapons appear in front of the pig and follow the camera:

1. **Drag** these four objects in the Hierarchy:
   - **HotDogLauncher**
   - **KetchupPistol**
   - **SpatulaSlapper**
   - **CondimentCluster**
2. **Drop** them onto **CameraHolder** (so they become children of CameraHolder)
3. With each weapon selected, set **Position** to something like (0.15, -0.25, 0.5) so they sit in front of the camera. Adjust to taste.

---

## 7. Save the Prefab

- Click the **&lt; Back** arrow at the top of the Hierarchy (or the breadcrumb) to exit Prefab mode.  
- Prefab is saved automatically.

---

## Controls in Game

- **1** – Primary (Hot Dog Launcher)  
- **2** – Secondary (Ketchup Pistol)  
- **3** – Melee (Spatula Slapper)  
- **4** – Throwable (Condiment Cluster)  
- **Scroll wheel** – Cycle weapons  
- **Left click** – Fire / attack  
- **R** – Reload (where applicable)

---

## Recommended weapon scales (so they don't fill the screen)

Use these on the **weapon object** (parent under CameraHolder), not the visual child:

- **HotDogLauncher:** Scale **0.12, 0.12, 0.12**
- **KetchupPistol:** Scale **0.15, 0.15, 0.15**
- **SpatulaSlapper:** Scale **0.15, 0.15, 0.15**
- **CondimentCluster:** Scale **0.15, 0.15, 0.15**

If the mesh is still huge, the **child** (Capsule/Cube/Sphere) can stay at (1,1,1) and the parent scale above will size it down.

---

## Crosshair (see where you're aiming)

1. Open the **ParkPicnic** scene (or whichever scene has the game HUD).
2. Find or create a **Canvas** (if none: Right-click Hierarchy > UI > Canvas).
3. Right-click the Canvas > **UI > Image**.
4. Rename it to **Crosshair**.
5. With **Crosshair** selected: **Rect Transform** – Set anchor to center (Alt+click the center preset). Pos X/Y = 0, **Width 8**, **Height 8** (use 6–10 if you want smaller or slightly bigger). **Image** – Set **Color** to white (or a bright color). For a simple dot, leave as a square; for a cross, use a crosshair sprite or add 4 child Images as short lines.
6. If you have an **HUDController** in the scene, assign this Image to its **Crosshair** field so it can be shown/hidden with the rest of the HUD.

---

## Notes

- **Hot Dog Launcher** and **Condiment Cluster** can work without prefabs (they have raycast/fallback behavior). For full projectiles/grenades you’d add prefabs to Resources later.
- **Ketchup Pistol** is hitscan and works as soon as it’s assigned.
- **Spatula Slapper** is melee; no muzzle point needed.
- If a slot is left empty, Weapon Manager will skip it and use the first weapon that is assigned.
- You can't damage yourself with the launcher, grenade, or projectile (shooter/thrower is excluded from splash damage).
