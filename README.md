# Pigs in a Blanket

A first-person multiplayer shooter in Unity with a picnic theme. Players control pigs and fight with food-themed weapons (Hot Dog Launcher, Ketchup Pistol, Spatula Slapper, Condiment Cluster). Built with **Photon PUN 2** for networking.

---

## Requirements

- **Unity** (2022 or compatible; project uses Unity 2022 layout).
- **Photon PUN 2** (included under `Assets/Photon`). You must set up a Photon App ID (see below).

---

## Photon setup

1. Create a free account at [photonengine.com](https://www.photonengine.com).
2. Create a **Photon PUN** app and copy the **App ID**.
3. In Unity: **Window → Photon Unity Networking → PUN Wizard** (or open `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings`).
4. Paste your **App ID** into the **AppId PUN** field and save.

Without this, the game cannot connect to Photon and multiplayer will not work.

---

## How to run

1. Open the project in Unity (open the folder that contains `Assets` and `ProjectSettings`).
2. Open the **lobby** scene (`Assets/Scenes/lobby.unity`). Ensure it is the first scene in **File → Build Settings** if you build; for Editor play, open lobby and press Play.
3. Enter a name and click **Connect**. Then either **Create Room** (enter a name and create) or **Join Random**.
4. In the room, the host clicks **Start Game** to load the **ParkPicnic** scene. All clients in the room are moved to the game and spawn as pigs with weapons.
5. **Controls:** 1–4 or scroll to switch weapons, left-click to fire/attack, R to reload. **Tab** toggles the scoreboard.

---

## Scene and setup docs

- **Lobby:** See [Assets/LOBBY_SETUP.md](Assets/LOBBY_SETUP.md) for required UI and prefab references.
- **Game (ParkPicnic):** See [Assets/PARK_PICNIC_SETUP.md](Assets/PARK_PICNIC_SETUP.md) for GameManager, HUD, scoreboard, spawn points, and PhotonView.
- **Weapons:** See [Assets/HOW_TO_ADD_WEAPONS.md](Assets/HOW_TO_ADD_WEAPONS.md) for adding weapons to the PigPlayer prefab.
- **Models and art:** See [Assets/MODELS_AND_ART.md](Assets/MODELS_AND_ART.md) for where to get 3D models and where to attach them.

---

## Build

- **File → Build Settings.** Ensure **lobby** and **ParkPicnic** (and any other game scenes) are in **Scenes In Build**. Build and run; start from the lobby scene so Photon and NetworkManager initialize.
