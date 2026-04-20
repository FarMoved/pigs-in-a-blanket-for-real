# Lobby Scene Setup

Use this checklist in the **lobby** scene so the LobbyManager works correctly.

---

## Scene name

- The lobby scene file is `lobby.unity`. Unity’s scene name is the filename without `.unity`, so it is **lobby** (lowercase).
- In **NetworkManager** (in the lobby scene or a persistent object), set **Lobby Scene Name** to `lobby` so “return to lobby” loads the right scene.

---

## LobbyManager references

Add a **LobbyManager** component to a GameObject in the lobby scene and assign the following.

### Connection panel (first screen)

- **Connection Panel** – GameObject that holds the “connect” UI (active when not connected).
- **Player Name Input** – TMP_InputField for the player’s name.
- **Connect Button** – Button that calls Connect.
- **Connection Status Text** – TextMeshProUGUI showing “Not connected”, “Connecting…”, “Connected”, etc.

### Lobby panel (room list)

- **Lobby Panel** – GameObject shown after connecting (room list screen).
- **Create Room Button** – Button to create a room (assigns a random 5-digit room code).
- **Join Random Button** – Button to join any open room.
- **Join Room Button** – Button that opens the "Join By Code" panel.
- **Room List Container** – Transform (e.g. under a Scroll View Content) where room entries are spawned.
- **Room Entry Prefab** – Prefab for one room row. Must have:
  - A **TextMeshProUGUI** (room name + player count).
  - A **Button** (e.g. “Join”) that LobbyManager will hook up to join that room.

### Join By Code panel

- **Join By Code Panel** – GameObject shown when the player clicks "Join Room" (panel with "Room Code:" and input).
- **Room Code Input** – TMP_InputField where the player types the 5-digit code. In Unity: add **UI → Input Field - TextMeshPro** as a child of JoinByCodePanel, set **Character Limit** to **5**, **Content Type** to **Integer Number** (or **Custom** with validation), and assign it to LobbyManager’s **Room Code Input**.
- **Join By Code Submit Button** – Button that joins the room using the entered code.
- **Join By Code Back Button** – Button that returns to the lobby panel.
- **Join By Code Status Text** – Optional TextMeshProUGUI for messages like "Joining...", "Room not found", etc.

### Room panel (inside a room, before starting game)

- **Room Panel** – GameObject shown when the player has joined a room.
- **Room Name Text** – TextMeshProUGUI showing the current room name.
- **Room Code Display Text** – TextMeshProUGUI showing "Room Code: 12345" so the host can share the code with friends.
- **Player List Container** – Transform where player entries are spawned.
- **Player Entry Prefab** – Prefab for one player row with a **TextMeshProUGUI** (player name; “(You)” and “[Host]” are added in code).
- **Start Game Button** – Button to load the game scene (only interactable for the master client).
- **Leave Room Button** – Button to leave the room and return to the lobby panel.

---

## NetworkManager

- The lobby (and game) scene must have a **NetworkManager** in the scene, or it must be in a scene loaded before the lobby and marked with **DontDestroyOnLoad** (it already does this in code).
- In NetworkManager, set **Lobby Scene Name** to `lobby` and **Game Scene Name** to `ParkPicnic` (or your game scene name).

---

## Return to lobby

- When a player leaves the room (Leave Room button), **NetworkManager.OnLeftRoom** runs and loads the lobby scene. No extra setup needed if the references above are set and the scene name is correct.

---

## Optional polish

- Room list: ensure the list refreshes when **OnRoomListUpdate** fires (LobbyManager already does this).
- “Back” from room: the Leave Room button returns to the lobby panel; no extra “Back” button is required.
- Status messages: Connection Status Text is updated by LobbyManager; you can style it or add more panels for errors.
