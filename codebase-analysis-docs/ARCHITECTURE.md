# Pigs in a Blanket – Codebase Architecture

High-level overview for developers and AI assistants.

---

## Project type

- **Unity** project (C#), first-person multiplayer shooter.
- **Photon PUN 2** for networking (rooms, RPCs, sync).
- **Scenes:** `lobby` (menus, matchmaking), `ParkPicnic` (gameplay).

---

## Scene flow

```
lobby (LobbyManager, NetworkManager)
  → Connect → Create/Join Room → Start Game (master)
  → ParkPicnic (GameSceneInitializer, GameManager, SpawnManager, TeamManager)
  → Leave Room / Match End → back to lobby
```

- **NetworkManager** is a singleton and uses `DontDestroyOnLoad` so it persists between lobby and game.
- **GameSceneInitializer** runs in ParkPicnic and calls `NetworkManager.SpawnLocalPlayer()` after a short delay when in a Photon room.

---

## Core scripts (by folder)

| Folder / area | Role |
|---------------|------|
| **Network/** | **NetworkManager** – Photon connect, create/join room, leave room, load game scene, spawn player. **LobbyManager** – Lobby UI (connect, room list, room panel, start game). **GameSceneInitializer** – Spawns local player when game scene loads. |
| **Player/** | **PlayerController** – FPS movement, look, jump; only processes input for `photonView.IsMine`. **PlayerHealth** – Health, damage (RPC), death, respawn; reports kills to GameManager. |
| **Weapons/** | **WeaponBase** – Ammo, fire rate, reload, GetShootCamera, GetOwnerViewID; base for all weapons. **WeaponManager** – On player; holds primary/secondary/melee/throwable, switches on 1–4/scroll, syncs via RPC; **RPC_TriggerMeleeSwing** for Spatula. **HotDogLauncher**, **KetchupPistol**, **SpatulaSlapper**, **CondimentCluster** – Implement PerformAttack; damage **PlayerHealth** and pass owner ViewID for kill credit. **HotDogProjectile**, **CondimentGrenade** – Projectile/explosion damage. |
| **Game/** | **GameManager** – Match state (WaitingForPlayers, Countdown, Playing, GameOver), timer (master only), team scores, countdown and match end RPCs; **ReportKill** from PlayerHealth. **Requires PhotonView on same GameObject.** **SpawnManager** – Red/blue spawn points or procedural spawns. **TeamManager** – Assigns local player to a team (custom properties), provides GetPlayerTeam. **DisableSceneCamera** – Utility to turn off scene camera when playing. |
| **UI/** | **HUDController** – Health, ammo, timer, score, kill feed, crosshair, countdown, game over, **death screen** (overlay + respawn countdown), damage vignette; subscribes to GameManager and local PlayerHealth/WeaponManager. **ScoreboardUI** – Tab to show/hide; lists players by team with kills/deaths from custom properties. **PlayerScoreEntry** – One row on the scoreboard. |

---

## Data flow (simplified)

- **Damage:** Weapon (or projectile) calls `PlayerHealth.TakeDamage(damage, ownerViewID)` on the hit player. Only the hit player’s instance should call this (Photon RPC then applies damage on all clients).
- **Kills:** When a player dies, **PlayerHealth** (on the dead player’s machine) calls **GameManager.ReportKill(attackerViewID, victimViewID)**. Master client updates team scores and broadcasts score + kill feed via RPCs.
- **Teams:** **TeamManager.AssignLocalPlayerToTeam()** is called from **NetworkManager.SpawnLocalPlayer()**. Team is stored in **PhotonNetwork.LocalPlayer.CustomProperties["Team"]**. **SpawnManager.GetSpawnPoint(team)** uses it for spawn position.
- **Kills/Deaths for scoreboard:** GameManager (or a related system) should set **CustomProperties["Kills"]** and **["Deaths"]** on players when they get a kill or die, so **ScoreboardUI** can read them. (If not yet implemented, scoreboard will show 0; the UI and **GetPlayerStat** are in place.)

---

## Key conventions

- **PhotonView ownership:** Player prefab (PigPlayer) has a PhotonView on the root. Weapons are children and use **GetComponentInParent&lt;PhotonView&gt;** for RPCs (e.g. SpatulaSlapper calls the player’s **WeaponManager.RPC_TriggerMeleeSwing**).
- **Local-only logic:** Movement, input, and camera are gated by `photonView.IsMine` (or equivalent) so only the local player is driven.
- **Master client:** GameManager runs the timer and match flow only on **PhotonNetwork.IsMasterClient**; state is synced via RPCs.

---

## Docs in the project

- **README.md** – How to open, run, Photon setup, build.
- **Assets/LOBBY_SETUP.md** – Lobby scene and LobbyManager references.
- **Assets/PARK_PICNIC_SETUP.md** – ParkPicnic scene (GameManager, HUD, scoreboard, spawn, death panel).
- **Assets/HOW_TO_ADD_WEAPONS.md** – Adding weapons to PigPlayer.
- **Assets/AMMO_UI_SETUP.md** – Ammo/weapon name UI.
- **Assets/MODELS_AND_ART.md** – Sourcing and attaching 3D models.
