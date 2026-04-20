# Balance and Audio

Short notes on tuning and sound setup.

---

## Balance (tuning in Inspector)

- **GameManager** – **Match Duration** (e.g. 240 s), **Pre Match Countdown** (e.g. 5 s). Adjust to taste.
- **PlayerHealth** – **Max Health** (default 100), **Respawn Delay** (default 2 s).
- **Weapons** – Each weapon script has damage, fire rate, ammo, range, etc. Tune in the prefab or scene:
  - **HotDogLauncher** – explosion damage, radius, projectile speed.
  - **KetchupPistol** – damage, fire rate.
  - **SpatulaSlapper** – damage, melee range, swing duration, fire rate (time between swings).
  - **CondimentCluster** – damage, explosion radius, fuse time, cluster count.
- **SpawnManager** – Red/blue spawn arrays or procedural **Map Center** / **Map Size** affect where players spawn.

---

## Audio

- **WeaponBase** (and subclasses) have **Fire Sound**, **Reload Sound**, **Empty Sound**. Assign **AudioClip**s in the PigPlayer prefab for each weapon so firing, reloading, and empty click play.
- **SpatulaSlapper** has **Swing Sound** and **Hit Sound**; assign in the prefab.
- Optional: add **AudioSource**s for ambient or music (e.g. on a persistent manager or in the scene). No code change required beyond assigning clips and volumes in the Inspector.
