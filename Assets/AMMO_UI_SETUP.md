# Ammo display (bottom-left or bottom-right)

Do this in the **ParkPicnic** scene so ammo shows in-game.

---

## 1. Open ParkPicnic and Canvas

- Open **ParkPicnic**.
- In the Hierarchy, select your **Canvas** (create one under UI if you don’t have it).

---

## 2. Ammo text (bottom-right)

1. Right-click **Canvas** → **UI** → **Text - TextMeshPro** (import TMP Essentials if asked).
2. Rename the new object to **AmmoText**.
3. Select **AmmoText**.
4. **Rect Transform**
   - Click the **anchor** box, hold **Alt**, and choose the **bottom-right** preset (right column, bottom row).
   - Set **Pos X**: `-120`, **Pos Y**: `40`, **Width**: `200`, **Height**: `40`.
   - Leave **Pivot** as 0.5, 0.5.
5. **TextMeshPro - Text**
   - Set **Text** to `0 / 0` (placeholder).
   - Set **Font Size** to `24` (or 20–28).
   - Set **Alignment** to right (horizontal) and middle (vertical).
   - Set **Color** to white (or any color you like).

---

## 3. (Optional) Weapon name above ammo

1. Right-click **Canvas** → **UI** → **Text - TextMeshPro**.
2. Rename to **WeaponNameText**.
3. **Rect Transform**
   - Anchor: **bottom-right** (Alt + bottom-right preset).
   - **Pos X**: `-120`, **Pos Y**: `85`, **Width**: `200`, **Height**: `30`.
4. **TextMeshPro - Text**
   - Set **Text** to `Weapon`.
   - **Font Size** `18`, alignment right, color white.

---

## 4. Connect to HUDController

1. Select **HUDController** in the Hierarchy.
2. In the Inspector, find **HUD Controller (Script)**.
3. Under **Ammo Display**:
   - Drag **AmmoText** into **Ammo Text**.
   - If you made it, drag **WeaponNameText** into **Weapon Name Text**.
4. Save the scene (**Ctrl+S**).

---

## Bottom-left instead of bottom-right

- For **AmmoText**: anchor **bottom-left**, **Pos X**: `120`, **Pos Y**: `40`.
- For **WeaponNameText**: anchor **bottom-left**, **Pos X**: `120`, **Pos Y**: `85`.
- Set text **Alignment** to **left** for both.

Ammo will show as `12 / 36` (current / reserve) and update when you shoot or reload.
