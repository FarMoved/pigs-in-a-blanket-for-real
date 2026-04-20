# ParkPicnic Scene Setup

Use this checklist so the game scene works correctly with networking, HUD, and match flow.

---

## Required GameObjects and components

### GameManager

- Create an empty GameObject (e.g. **GameManager**).
- Add the **GameManager** script.
- **Add a PhotonView component** to the same GameObject (required for RPCs: timer, score, countdown, match end). In PhotonView, you can leave "Observed Components" empty or add GameManager if you need sync; the important part is the View ID for RPCs.
- Assign (or leave empty to auto-find):
  - **Spawn Manager**
  - **HUD Controller**
- Match settings (Match Duration, Pre Match Countdown) are set in the inspector.

### SpawnManager

- Add a **SpawnManager** component to a GameObject (e.g. **SpawnManager**).
- Assign **Red Spawn Points** and **Blue Spawn Points**: drag Transform(s) for each team (empty GameObjects placed in the map). If left empty, spawns are generated procedurally from Map Center / Map Size.

### GameSceneInitializer

- Add **GameSceneInitializer** to a GameObject that is active when the ParkPicnic scene loads (e.g. on the same object as GameManager or a dedicated "SceneSetup" object). It spawns the local player after a short delay when in a Photon room.

### NetworkManager

- **NetworkManager** should already exist from the lobby (DontDestroyOnLoad). If you start play from ParkPicnic for testing, ensure a NetworkManager exists in the scene and that **Game Scene Name** is `ParkPicnic`.

### TeamManager

- Add **TeamManager** to a GameObject (e.g. **TeamManager**). Optional: assign Red/Blue team materials or colors. Used when spawning to assign the local player to a team and for score/kill attribution.

---

## HUD (HUDController)

- Create or use a **Canvas** with a **HUDController** component.
- Assign:
  - **Health:** Health Slider, Health Text, Health Fill (Image for bar color).
  - **Ammo:** Ammo Text, optional Weapon Name Text, optional Weapon Icon.
  - **Timer:** Timer Text (e.g. "4:00").
  - **Score:** Red Score Text, Blue Score Text.
  - **Kill Feed:** Kill Feed Container (Transform), Kill Feed Entry Prefab (prefab with TextMeshProUGUI).
  - **Crosshair:** Crosshair (Image).
  - **Countdown & Game Over:** Countdown Text, Game Over Panel (panel with Game Over Text).
  - **Death Screen:** Death Panel (e.g. full-screen overlay), Death Message Text ("You were eliminated!"), Respawn Countdown Text (e.g. "Respawning in 2...").
  - **Damage Indicator:** Damage Vignette (full-screen Image, red tint when hit).

HUDController finds GameManager at Start and subscribes to timer and score. It is wired to the local player when **NetworkManager.SpawnLocalPlayer** calls **hud.SetupForLocalPlayer(health, weapons)**.

---

## Scoreboard (ScoreboardUI)

- Add **ScoreboardUI** to a GameObject (e.g. under the same Canvas).
- Assign:
  - **Scoreboard Panel** – GameObject that contains the scoreboard (shown when Tab is held).
  - **Toggle Key** – Tab (default).
  - **Red Team Container** / **Blue Team Container** – Transforms where player rows are spawned.
  - **Player Entry Prefab** – Prefab with **PlayerScoreEntry** (or at least TextMeshProUGUI for fallback).
  - **Red Team Score Header** / **Blue Team Score Header** – Optional TextMeshPro for "RED TEAM - 0".

ScoreboardUI already toggles on **Tab down** and hides on **Tab up**.

---

## Match end and return to lobby

- **Game Over** is shown by HUDController when GameManager calls **ShowGameOver(winner)** (from RPC_EndMatch). Ensure **Game Over Panel** and **Game Over Text** are assigned.
- To add "Return to Lobby": add a button on the Game Over Panel that calls **NetworkManager.Instance.LeaveRoom()**. OnLeftRoom will load the lobby scene (see LOBBY_SETUP.md).

---

## Build settings

- Include **lobby** and **ParkPicnic** (and any other game scenes) in **File → Build Settings → Scenes In Build** so loading by name works in a build.
