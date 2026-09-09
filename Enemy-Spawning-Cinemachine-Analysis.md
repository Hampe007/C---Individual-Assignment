# Enemy Spawning and Cinemachine Compatibility Analysis

Project version: Unity 6000.3.23f1 / Unity 6.3 LTS  
Analysis scope: Entire Unity project, with particular attention to `Assets/`, gameplay scripts, scenes, prefabs, build settings, and package configuration.  
No project files were modified as part of the original analysis.

# 1. Relevant files

There is one enemy-spawning implementation and one camera-visibility test in the project.

| Full relative path | Class/asset | Purpose and why it matters |
|---|---|---|
| `Assets/Project/Scripts/Enemies/DifficultyDirector.cs` | `DifficultyDirector` | Decides when and how many enemies to request. It calls `HordeManager.TrySpawnEnemy()`. |
| `Assets/Project/Scripts/Enemies/HordeManager.cs` | `HordeManager` | Generates candidate positions, applies minimum-distance and camera-visibility rejection, initializes runtime state, and asks the view pool for a visual. This contains the actual spawn exclusion logic. |
| `Assets/Project/Scripts/Enemies/EnemyRuntime.cs` | `EnemyRuntime` | Stores position, health, behavior, visual type, and initial `Chase` state for each accepted spawn. |
| `Assets/Project/Scripts/Enemies/EnemyViewSystem.cs` | `EnemyViewSystem` | Owns three `ObjectPool<GameObject>` instances. Gets or instantiates the selected visual and places it at the accepted position. |
| `Assets/Project/Scripts/Enemies/SyncEnemyViewsJob.cs` | `SyncEnemyViewsJob` | Synchronizes pooled view transforms with runtime enemy positions after movement. It does not choose or validate spawn positions. |
| `Assets/Project/Scripts/Enemies/MoveEnemiesJob.cs` | `MoveEnemiesJob` | Moves accepted enemies toward the player. It has no camera or spawn-visibility logic. |
| `Assets/Project/Scripts/Enemies/BuildSpatialGridJob.cs` | `BuildSpatialGridJob` | Builds the neighbor grid used after spawning. It has no spawn-position logic. |
| `Assets/Project/Scripts/Enemies/SpatialGrid.cs` | `SpatialGrid` | Maps active enemy positions to grid cells. It has no spawning or camera logic. |
| `Assets/Project/Scripts/Enemies/EnemyBehaviourType.cs` | `EnemyBehaviourType` | Defines `Swarm`, `Charger`, and `Brute`, selected before each spawn request. |
| `Assets/Project/Scripts/Enemies/EnemyVisualType.cs` | `EnemyVisualType` | Maps behaviors to pooled visual prefabs. |
| `Assets/Project/Scripts/Enemies/EnemyState.cs` | `EnemyState` | Supplies the initial `Chase` state. |
| `Assets/Project/Scripts/Camera/CameraFollow.cs` | `CameraFollow` | Moves its own transform in `LateUpdate`. Nothing in the spawn system references this class or its fields. |
| `Assets/Project/Scripts/Player/PlayerMovement.cs` | `PlayerMovement` | Uses a serialized camera, falling back to `Camera.main`, to create a mouse ray. This is input-related and independent of enemy spawning. |
| `Assets/Scenes/Game.unity` | Gameplay scene | Wires `HordeManager._camera` and `PlayerMovement.worldCamera` to the actual Unity `Camera` component on `Main Camera`. It also contains the current Inspector overrides. |
| `Assets/Project/Prefabs/Enemies/EnemyView_Imp.prefab` | Imp view | Visual-only pooled enemy root. |
| `Assets/Project/Prefabs/Enemies/EnemyView_Lycan.prefab` | Lycan view | Visual-only pooled enemy root. |
| `Assets/Project/Prefabs/Enemies/EnemyView_Tidebreaker.prefab` | Tidebreaker view | Visual-only pooled enemy root. |
| `Assets/Scenes/SampleScene 1.unity` | Sample scene | Contains another `CameraFollow` attachment, but no `HordeManager`. It is not enabled in Build Settings. |
| `Assets/Scenes/MainMenu.unity` | Main-menu scene | Has its own tagged Main Camera, but no enemy-spawning system. |
| `ProjectSettings/EditorBuildSettings.asset` | Build configuration | Only `MainMenu.unity` and `Game.unity` are enabled. |
| `Packages/packages-lock.json` | Package lock | Cinemachine 3.1.7 is available transitively through the Character Animation feature package. |
| `Packages/manifest.json` | Package manifest | Cinemachine is not a direct dependency, but is present through `com.unity.feature.characters-animation`. |

No separate spawn points, spawn-point components, alternative spawn managers, enemy `MonoBehaviour` initialization scripts, or other enemy pools were found. The enemy prefabs are visual objects; simulation state is centralized in `HordeManager` and `EnemyRuntime`.

# 2. Current spawn flow

```text
Unity Update loop
  -> DifficultyDirector.Update()
      -> first ready frame:
           SpawnEnemies(_startingEnemies)
         later:
           wait for _spawnTimer
           calculate interval and batch size
           SpawnEnemies(batchSize)
      -> DifficultyDirector.SpawnEnemies(amount)
          -> GetBehaviour()
          -> HordeManager.TrySpawnEnemy(behaviour)
              -> HordeManager.TryGetSpawnPosition(out position)
                  -> up to 24 attempts
                  -> GetSpawnZone()
                  -> generate point on fixed world-space square perimeter
                  -> reject if closer than _minSpawnDistance to _target
                  -> IsVisible(candidate)
                      -> _camera.WorldToViewportPoint(candidate + 0.9y)
                      -> reject if inside padded viewport
                  -> RememberSpawnZone(zone)
              -> GetVisual(behaviour)
              -> GetHealth(behaviour)
              -> new EnemyRuntime(...)
              -> EnemyViewSystem.AddView(position, visual)
                  -> selected ObjectPool<GameObject>.Get()
                      -> Instantiate(prefab), or reuse inactive view
                      -> SetActive(true)
                  -> set view.transform.position
              -> write runtime state into both NativeArrays
              -> increment _activeEnemyCount

Subsequent HordeManager.Update()
  -> Simulate()
      -> BuildSpatialGridJob
      -> MoveEnemiesJob
      -> EnemyViewSystem.ScheduleSync()
          -> SyncEnemyViewsJob
      -> apply player damage
      -> swap current/next NativeArrays
```

`DifficultyDirector` decides when spawning should happen.

`HordeManager.TryGetSpawnPosition()` generates and validates the candidate.

The scene starts with `_startingEnemies = 1`, despite the source default being 20. Later batches start at 2 and rise toward 10 while the interval falls from 1.25 to 0.4 seconds over 300 seconds.

# 3. Exact visibility logic

The complete candidate-generation method is:

```csharp
private bool TryGetSpawnPosition(out float3 position)
{
    for (int attempt = 0; attempt < 24; attempt++)
    {
        int zone = GetSpawnZone();
        float angle = zone * 45f + _random.NextFloat(-_zoneJitter, _zoneJitter);
        float2 direction = new(math.sin(math.radians(angle)), math.cos(math.radians(angle)));

        float edge = _spawnEdge + _random.NextFloat(-_spawnDepth, _spawnDepth);
        float scale = edge / math.max(math.abs(direction.x), math.abs(direction.y));

        float3 candidate = new(direction.x * scale, 0f, direction.y * scale);
        float minDistanceSq = _minSpawnDistance * _minSpawnDistance;

        if (math.distancesq(candidate, (float3)_target.position) < minDistanceSq)
            continue;

        if (IsVisible(candidate))
            continue;

        RememberSpawnZone(zone);
        position = candidate;
        return true;
    }

    position = default;
    return false;
}
```

The visibility test is:

```csharp
private bool IsVisible(float3 position)
{
    Vector3 point = new(position.x, position.y + 0.9f, position.z);
    Vector3 viewport = _camera.WorldToViewportPoint(point);

    const float padding = 0.05f;

    return viewport.z > 0f
        && viewport.x >= -padding
        && viewport.x <= 1f + padding
        && viewport.y >= -padding
        && viewport.y <= 1f + padding;
}
```

The conditions are:

- Candidate generation uses eight 45-degree zones around the world origin.
- The gameplay scene overrides `_zoneJitter` to 15 degrees, so each zone uses its center angle plus or minus 15 degrees.
- `_spawnEdge = 55` and `_spawnDepth = 2`, giving a square half-extent between approximately 53 and 57 world units.
- This is an axis-aligned square perimeter centered on world `(0,0,0)`. It is not centered on the player or camera.
- Candidate height is always `y = 0`.
- Candidates closer than 30 world units to `_target.position` are rejected. In `Game.unity`, `_target` is the `PlayerTarget` transform.
- The distance check uses full 3D squared distance, although normal gameplay keeps both positions near ground level.
- Visibility samples one point 0.9 units above the enemy root.
- `viewport.z > 0` requires the point to be in front of the camera.
- Normal visible viewport coordinates are `x/y = 0..1`.
- The rejection range is enlarged to `-0.05..1.05`, giving a 5% viewport margin on every side.
- There are at most 24 complete candidate attempts per enemy request.
- If all 24 candidates fail, `TrySpawnEnemy()` returns `false`. `DifficultyDirector` ignores that result, so that requested enemy is skipped.
- Accepted spawn zones are tracked so the same zone cannot be accepted more than twice consecutively.

Answers C-G:

- **C:** A position is rejected as visible when its elevated sample point is in front of the camera and inside the padded viewport rectangle.
- **D:** The test uses the serialized `_camera` field. In `Game.unity`, that field points to the Unity `Camera` on the active, tagged `Main Camera`.
- **E:** It uses the real camera's world-to-viewport projection, including its transform, perspective/orthographic mode, lens, aspect ratio, and viewport. It does not calculate a hard-coded view from player distance. It approximates the enemy itself with one point rather than testing renderer bounds.
- **F:** The `Camera` object reference is retained, but no transform, matrix, frustum planes, or viewport data is cached. `WorldToViewportPoint()` reads the camera state when called. Because spawning runs in `Update` and `CameraFollow` runs in `LateUpdate`, the test normally occurs before that frame's final camera movement.
- **G:** There is a 5% normalized viewport margin. There is no renderer-size-aware or world-unit camera margin. The 30-unit player distance is a separate restriction.

Additional limitations of the current test:

- It does not test the whole Imp, Lycan, or Tidebreaker renderer bounds.
- It does not test occlusion. Even an on-screen point hidden behind geometry is rejected.
- It does not check the camera culling mask or enemy layer.
- It checks `z > 0`, but not the camera's near or far clip distances.
- A large FOV or distant camera can make most or all of the fixed spawn square visible, causing repeated spawn failures rather than allowing an on-screen spawn.

# 4. Camera dependencies

## a) Enemy spawning

`HordeManager` has:

```csharp
[SerializeField] private Camera _camera;
```

The gameplay scene assigns that field directly to Main Camera's Unity `Camera` component. Spawning calls `_camera.WorldToViewportPoint()`.

It does not call `Camera.main`, access `_camera.transform` explicitly, or know about `CameraFollow`. The projection method internally uses the real camera transform and projection.

## b) Aiming/input

`PlayerMovement` has a serialized `worldCamera`. In `Game.unity`, it is also assigned to Main Camera's Unity `Camera` component. If unassigned, it falls back once during `Awake`:

```csharp
if (worldCamera == null)
{
    worldCamera = Camera.main;
}
```

It then uses:

```csharp
Ray ray = worldCamera.ScreenPointToRay(mousePosition);
```

This will also follow Cinemachine automatically as long as the same Unity Camera is used.

## c) UI

No project UI script references `Camera.main`, `Camera`, `CameraFollow`, or camera transforms. The gameplay canvases inspected are configured as screen-space overlay with serialized `m_Camera: {fileID: 0}`.

## d) Other gameplay

`CameraFollow` is the only other gameplay camera dependency. It moves Main Camera in `LateUpdate`.

Every `CameraFollow` reference found was:

- Its class declaration.
- Its attachment to Main Camera in `Assets/Scenes/Game.unity`.
- Its attachment to Main Camera in `Assets/Scenes/SampleScene 1.unity`.

No script queries `CameraFollow`, calls `GetComponent<CameraFollow>()`, or reads:

- `CameraFollow.target`
- `CameraFollow.offset`
- `CameraFollow.followSpeed`
- Whether `CameraFollow` is enabled

The spawn system has no hard-coded `(0,12,-8)` offset and never computes `camera position = player position + offset`.

Current `Game.unity` wiring is:

- Main Camera: perspective, 60-degree FOV, full normalized viewport.
- Initial camera transform: `(0, 12.75, -11)`, rotation approximately 52 degrees on X.
- `CameraFollow.target`: `PlayerTarget`.
- `CameraFollow.offset`: `(0,12,-8)`.
- `HordeManager._target`: the same `PlayerTarget`.
- `HordeManager._camera`: Main Camera's Unity `Camera`.
- `PlayerMovement.worldCamera`: the same Unity `Camera`.

Only `_target` and `_camera` matter for spawning.

# 5. Cinemachine compatibility

**A) Existing spawning code should work unchanged with Cinemachine.**

CinemachineBrain applies the selected Cinemachine camera state to the attached real Unity `Camera`. `HordeManager` already references that real Camera component, so `WorldToViewportPoint()` will automatically use the position, rotation, FOV, projection, and aspect produced by Cinemachine.

The important conditions are:

- Keep the existing Main Camera GameObject and Unity `Camera` component.
- Attach `CinemachineBrain` to that object.
- Preserve its `MainCamera` tag.
- Keep `HordeManager._camera` assigned to that Unity `Camera`.
- Do not assign a `CinemachineCamera` component to the spawn system.
- Disable or remove `CameraFollow` once Cinemachine controls the camera, so two components do not write the same transform.

Cinemachine 3.1.7 is present in the package lock, but no CinemachineBrain or CinemachineCamera is currently serialized in the inspected `Assets` scenes or prefabs. The migration has not yet been saved into the project state inspected here.

There is a timing caveat. `DifficultyDirector` spawns in `Update`. Current `CameraFollow` moves in `LateUpdate`, and CinemachineBrain's normal application also occurs in `LateUpdate`. Therefore, an accepted point could theoretically enter the rendered viewport when the camera moves later in the same frame. The existing 5% padding reduces this risk but does not mathematically eliminate it during fast movement, low frame rates, camera cuts, or large same-frame lens changes. This is an existing design limitation, not a dependency on `CameraFollow`.

# 6. Recommended migration

1. Keep the existing `Main Camera` GameObject, tag, Unity `Camera`, AudioListener, and URP camera data.
2. Add `CinemachineBrain` to that Main Camera.
3. Create a separate Cinemachine camera and use `PlayerTarget` as its tracking/follow target.
4. To reproduce the old framing, start with an equivalent `(0,12,-8)` positional offset, fixed approximately 52-degree downward rotation, perspective projection, and 60-degree FOV.
5. Disable or remove `CameraFollow` after the Cinemachine camera is active.
6. Verify in the Inspector that:
   - `HordeManager._camera` still references Main Camera's Unity `Camera`.
   - `HordeManager._target` still references `PlayerTarget`.
   - `PlayerMovement.worldCamera` still references Main Camera's Unity `Camera`.
7. Leave `HordeManager`, `DifficultyDirector`, and `EnemyViewSystem` unchanged.

No duplicated camera math or Cinemachine-specific spawn code is needed.

If testing exposes same-frame pop-in, the smallest later hardening would be to validate or execute spawning after CinemachineBrain has applied its final camera pose for the frame. That would improve the current guarantee but would change the existing timing behavior, so it is separate from the initial migration.

# 7. Risks / tests

- Test at all four edges and corners of the Game view. Confirm no enemy's visible mesh appears when its pooled view activates.
- Temporarily visualize or log accepted spawn points and verify their projected sample point is outside `-0.05..1.05` at render time.
- Confirm enemies spawn just beyond the 5% margin rather than leaving an unnecessarily large empty band.
- Increase and decrease Cinemachine camera distance. Confirm rejection follows the new real viewport and watch for spawn starvation when the fixed 53-57-unit perimeter becomes fully visible.
- Change the active Cinemachine lens FOV substantially. Confirm the exclusion region changes without editing `HordeManager`.
- Test wide, standard, tall, and resized Game views. `WorldToViewportPoint()` should adapt to each aspect ratio.
- Run at a deliberately low frame rate while moving at maximum speed. Watch for a spawn accepted in `Update` entering view after the Brain moves in `LateUpdate`.
- Test high and low Cinemachine damping, camera cuts, and blends.
- Move continuously toward and along the world-space spawn square. Confirm the 30-unit player-distance rejection still works and that enough valid candidates remain.
- Test all three enemy visuals, especially the larger Tidebreaker, because visibility checks only a point at `y + 0.9`, not the full renderer bounds.
- Confirm click movement still raycasts correctly after `CameraFollow` is disabled.
