# Game grading checklist

Updated: 2026-10-12 · **8 verified / 13 requirements**

Checked means the behaviour was verified in Unity Play mode. Unchecked means it still needs work or verification—not necessarily that it is missing. These checks do not guarantee the examiner's grade.

## G-Pass

- [ ] **Keyboard and mouse movement** — Both are implemented. The automated input test did not establish movement, so this stays open. Verify WASD/arrows, right-click movement, stopping, and keyboard priority in the focused Game view.
- [x] **Enemies spawn, move, and attack** — The game spawned 5 enemies naturally. Swarm, Charger, and Brute enemies each moved and damaged the player in runtime tests.
- [x] **Automatic weapon attacks and cooldown** — The sword killed an enemy without attack input. Repeated hits followed its upgraded 0.9-second cooldown.
- [x] **Death and restart reset attributes** — Death opened the game-over menu and paused play. Restart restored starting health, weapons, progression, upgrades, lantern state, and difficulty. Restart uses a button.
- [x] **Player can acquire upgrades** — Tested damage, speed, max health, cooldown, additional targets, lantern unlock, and lantern range. All changed the intended attributes.
- [x] **Upgrade choices on level-up** — Enemy kills triggered levelling and opened the upgrade menu with multiple available choices. Button callbacks purchased upgrades, spent one point, and resumed play.
- [x] **Difficulty increases with time** — Tested the director at early and late elapsed times: batches increased from 2 to 4 enemies, and intervals fell from about 2 seconds to 0.9 seconds.
- [ ] **Visible score, XP, and health UI** — Score text and XP/health bars are connected. Verify that all three are visible, readable, and update correctly during play. Numeric XP/health text is absent; add it if the examiner expects numbers rather than bars.
- [ ] **Persistent high score visible in MainMenu** — Saving and menu display are implemented. Verify a new best score appears after death, survives closing and reopening the application, and is not overwritten by a lower score.

## VG-Pass

- [x] **Three enemy types with different behaviours** — Swarm chased and dealt contact damage; Charger telegraphed, charged, and recovered; Brute chased, wound up, attacked, and recovered. All three were tested at runtime.
- [ ] **Gamepad movement** — Left-stick binding and a Gamepad control scheme exist. Connect a real controller and verify movement, stick release, analog speed, and switching between controller and keyboard/mouse.
- [x] **Enemy and upgrade creation tool** — The Content Creator successfully created Swarm, Charger, and Brute assets plus a speed upgrade, and rejected duplicate enemy names. Temporary test assets were removed.
- [ ] **Polished look and smooth performance** — Visual/audio features and optimizations exist, but need assessment. Review the complete game flow and profile a standalone build under the supported worst-case conditions.

## What's left

| Task | What counts as completion |
| --- | --- |
| Verify movement | WASD/arrows and right-click move the character correctly; stopping and keyboard priority work. |
| Verify the HUD | Score, XP, and health remain readable and update after kills, damage, levelling, and restart. Resolve whether numeric XP/health is expected. |
| Test high-score persistence | A new best appears in MainMenu after a complete application restart; a lower score cannot replace it. |
| Test a physical gamepad | Movement works in all directions, stops on release, supports analog speed, and survives input-device switching. |
| Review polish and performance | Menus, tutorial, combat, upgrades, death, and restart look and feel consistent. The standalone build meets a recorded frame-rate goal on the target hardware, including the 500-enemy cap, heavy effects, and repeated restarts. Fix observed stutters, errors, or memory growth. |

## Useful details

- **Restart was verified:** health/max health 100, speed 5, sword damage 20, cooldown 1 second, one target, level 1, zero XP/score/skill points, and no purchased upgrades or unlocked lantern.
- **“Extra sword” currently means one more enemy hit per attack**, not another visible sword. Make sure the upgrade description communicates this.
- **The creation tool creates assets.** New enemies still need registration in HordeManager and DifficultyDirector; upgrades need skill-tree nodes and connected buttons.
- **Lycan T3 now references the correct Lycan prefab.** The earlier discrepancy is resolved in the current working copy.
- Tests used controlled Play mode scenarios, actual attack animations and upgrade callbacks, and selected elapsed times for difficulty. A complete standalone playthrough and performance capture remain outstanding.

## Evidence

Reviewed [Game.unity](Assets/Scenes/Game.unity), [PlayerControls.inputactions](Assets/PlayerControls.inputactions), gameplay scripts, enemy/upgrade assets, and scene references. Runtime tests confirmed the checked behaviours above. The original unsaved MainMenu scene was preserved.
