# Tower animations

The four tower prefabs use Spine 4.2 skeletons from `export 2`:

| Prefab | Skeleton |
| --- | --- |
| BaseTower | tower_green |
| RapidTower (Ice Tower) | tower_blue |
| CannonTower (fireTower) | tower_red |
| BeamTower (ElectroTower) | tower_yellow |

Run **First Game > Import Tower Spine Animations** to update the imported assets
and existing prefabs without rebuilding scenes or overwriting game settings.
The prototype scene builder also installs these visuals when recreating prefabs.
Run **First Game > Validate Tower Spine Animations** to check all four attack
directions, return to idle, nonempty meshes, and level scaling.

`TowerAnimationController` selects an attack quadrant from the target position,
then returns to the exported left/right idle animation. Attack playback accelerates
when needed to fit the existing shot interval. Damage timing and attack components
are unchanged. The visual root grows by `1.1^(level-1)` independently of Spine bones.

Only tower skeletons are imported. Enemy and projectile visuals are unchanged.
The atlas is shared, uses straight alpha, and is imported without compression or
power-of-two resizing so rotated atlas regions keep their original coordinates.

## Runtime provenance

`Assets/Spine` contains the official spine-unity 4.2 runtime (2026-05-29), excluding
the example project. Source: https://esotericsoftware.com/spine-unity-download
Package: https://esotericsoftware.com/files/runtimes/unity/spine-unity-4.2-2026-05-29.unitypackage

The upstream license notices are retained in the runtime source. Spine runtime
use is subject to the Spine Runtimes License; see those notices for terms.

Local Unity 6.6 compatibility changes replace the removed integer object-ID APIs
in the upstream editor utilities with `EntityId` APIs, use the V2 hierarchy drag
callbacks, and use Unity object equality/hash codes in the Mecanim clip comparer.
