using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player keybind preferences persisted via PlayerPrefs.
/// </summary>
public enum KeybindId
{
    WeaponSlot1,
    WeaponSlot2,
    WeaponSlot3,
    WeaponSlot4,
    Inspect,
    Reload,
    Scoreboard,
    SizzleStep,
    WallSkim,
    LaunchPatty
}

public static class GameKeybinds
{
    public struct BindingInfo
    {
        public KeybindId Id;
        public string Label;

        public BindingInfo(KeybindId id, string label)
        {
            Id = id;
            Label = label;
        }
    }

    public static readonly BindingInfo[] AllBindings =
    {
        new BindingInfo(KeybindId.WeaponSlot1, "Weapon Slot 1"),
        new BindingInfo(KeybindId.WeaponSlot2, "Weapon Slot 2"),
        new BindingInfo(KeybindId.WeaponSlot3, "Weapon Slot 3"),
        new BindingInfo(KeybindId.WeaponSlot4, "Weapon Slot 4"),
        new BindingInfo(KeybindId.Inspect, "Inspect"),
        new BindingInfo(KeybindId.Reload, "Reload"),
        new BindingInfo(KeybindId.Scoreboard, "Scoreboard"),
        new BindingInfo(KeybindId.SizzleStep, "Sizzle Step"),
        new BindingInfo(KeybindId.WallSkim, "Wall Skim"),
        new BindingInfo(KeybindId.LaunchPatty, "Launch Patty")
    };

    private static readonly KeyCode[] DefaultKeys =
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.V,
        KeyCode.R,
        KeyCode.Tab,
        KeyCode.Q,
        KeyCode.E,
        KeyCode.F
    };

    private static KeyCode[] keys;

    public static void Load()
    {
        if (keys == null || keys.Length != AllBindings.Length)
            keys = new KeyCode[AllBindings.Length];

        for (int i = 0; i < AllBindings.Length; i++)
        {
            string prefKey = "Keybind_" + AllBindings[i].Id;
            int stored = PlayerPrefs.GetInt(prefKey, (int)DefaultKeys[i]);
            keys[i] = (KeyCode)stored;
        }
    }

    public static KeyCode Get(KeybindId id)
    {
        EnsureLoaded();
        int index = (int)id;
        if (index < 0 || index >= keys.Length)
            return KeyCode.None;
        return keys[index];
    }

    public static bool Set(KeybindId id, KeyCode key)
    {
        if (key == KeyCode.None)
            return false;

        EnsureLoaded();
        int index = (int)id;
        if (index < 0 || index >= keys.Length)
            return false;

        keys[index] = key;
        PlayerPrefs.SetInt("Keybind_" + id, (int)key);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Returns bindings that share a key with another action (excluding None).</summary>
    public static HashSet<KeybindId> GetConflictingBindings()
    {
        EnsureLoaded();
        var conflicts = new HashSet<KeybindId>();
        var usedByKey = new Dictionary<KeyCode, List<KeybindId>>();

        for (int i = 0; i < AllBindings.Length; i++)
        {
            KeybindId id = AllBindings[i].Id;
            KeyCode key = keys[i];
            if (key == KeyCode.None)
                continue;

            if (!usedByKey.TryGetValue(key, out List<KeybindId> list))
            {
                list = new List<KeybindId>();
                usedByKey[key] = list;
            }
            list.Add(id);
        }

        foreach (KeyValuePair<KeyCode, List<KeybindId>> pair in usedByKey)
        {
            if (pair.Value.Count < 2)
                continue;
            foreach (KeybindId id in pair.Value)
                conflicts.Add(id);
        }

        return conflicts;
    }

    /// <summary>If key is already used by another binding, returns that binding.</summary>
    public static bool TryGetOtherBindingForKey(KeyCode key, KeybindId excludeId, out KeybindId otherId)
    {
        otherId = default;
        if (key == KeyCode.None)
            return false;

        EnsureLoaded();
        for (int i = 0; i < AllBindings.Length; i++)
        {
            KeybindId id = AllBindings[i].Id;
            if (id == excludeId)
                continue;
            if (keys[i] == key)
            {
                otherId = id;
                return true;
            }
        }

        return false;
    }

    public static string GetBindingLabel(KeybindId id)
    {
        for (int i = 0; i < AllBindings.Length; i++)
        {
            if (AllBindings[i].Id == id)
                return AllBindings[i].Label;
        }

        return id.ToString();
    }

    public static bool GetKeyDown(KeybindId id)
    {
        KeyCode key = Get(id);
        return key != KeyCode.None && Input.GetKeyDown(key);
    }

    public static string FormatKey(KeyCode key)
    {
        if (key == KeyCode.None)
            return "—";

        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            return ((int)key - (int)KeyCode.Alpha0).ToString();

        if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
            return "NUM " + ((int)key - (int)KeyCode.Keypad0);

        switch (key)
        {
            case KeyCode.LeftShift: return "L SHIFT";
            case KeyCode.RightShift: return "R SHIFT";
            case KeyCode.LeftControl: return "L CTRL";
            case KeyCode.RightControl: return "R CTRL";
            case KeyCode.LeftAlt: return "L ALT";
            case KeyCode.RightAlt: return "R ALT";
            case KeyCode.Space: return "SPACE";
            case KeyCode.Tab: return "TAB";
            case KeyCode.Escape: return "ESC";
            case KeyCode.Return: return "ENTER";
            case KeyCode.Backspace: return "BACKSPACE";
            case KeyCode.CapsLock: return "CAPS";
            case KeyCode.UpArrow: return "UP";
            case KeyCode.DownArrow: return "DOWN";
            case KeyCode.LeftArrow: return "LEFT";
            case KeyCode.RightArrow: return "RIGHT";
        }

        string name = key.ToString();
        if (name.StartsWith("Mouse"))
            return name.ToUpperInvariant().Replace("MOUSE", "M");

        return name.ToUpperInvariant();
    }

    private static void EnsureLoaded()
    {
        if (keys == null || keys.Length != AllBindings.Length)
            Load();
    }
}
