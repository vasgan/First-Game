using System.Collections.Generic;
using UnityEngine;

namespace FirstGame
{
    /// <summary>Builds the currently selected level from its data asset, not scene-specific coordinates.</summary>
    public static class LevelLayoutApplier
    {
        public static readonly Color BackgroundEdgeColor = new(28f / 255f, 8f / 255f, 64f / 255f, 1f);
        private const float TileStep = 1f;
        private const float BorderOffset = TileStep * .5f;

        public static void Apply(LevelDefinition level, WaveController waves)
        {
            if (!level || !waves || level.route == null || level.route.Length < 2 ||
                level.buildSpots == null || level.buildSpots.Length == 0 || !level.waveConfig)
            {
                Debug.LogError("Selected level requires route, build spots and wave data.");
                return;
            }

            GameObject routeRoot = GameObject.Find("Route") ?? new GameObject("Route");
            Sprite spotSprite = FindSprite("BuildSpot");
            Sprite baseSprite = FindSprite("Base");
            TowerFactory factory = Object.FindFirstObjectByType<TowerFactory>();
            if (!HasCompletePalette(level.routePalette) || !spotSprite || !baseSprite || !factory)
            {
                Debug.LogError("Selected level requires its complete route-tile palette, build spot, base, and tower factory visuals.");
                return;
            }

            ApplyInactiveFieldFill();
            ClearChildren(routeRoot.transform);
            foreach (BuildSpot oldSpot in Object.FindObjectsByType<BuildSpot>(FindObjectsSortMode.None))
                Object.Destroy(oldSpot.gameObject);

            Transform[] route = new Transform[level.route.Length];
            for (int index = 0; index < level.route.Length; index++)
            {
                GameObject waypoint = new($"Waypoint_{index:00}");
                waypoint.transform.SetParent(routeRoot.transform);
                waypoint.transform.position = level.route[index];
                route[index] = waypoint.transform;
            }

            DrawRoute(level.route, level.routePalette, routeRoot.transform);

            GameObject baseObject = GameObject.Find("Base");
            if (baseObject)
            {
                baseObject.transform.position = level.route[^1];
                if (baseObject.TryGetComponent(out SpriteRenderer baseRenderer))
                    baseRenderer.enabled = false;
            }

            foreach (Vector2 position in level.buildSpots)
            {
                GameObject spot = new("BuildSpot");
                spot.transform.position = position;
                spot.transform.localScale = Vector3.one * 5f;
                SpriteRenderer marker = spot.AddComponent<SpriteRenderer>();
                marker.sprite = spotSprite;
                marker.color = new Color(.8f, .85f, 1f);
                marker.sortingOrder = 4;
                CircleCollider2D collider = spot.AddComponent<CircleCollider2D>();
                collider.radius = .42f;
                BuildSpot build = spot.AddComponent<BuildSpot>();
                build.Configure(factory, marker);
            }

            waves.Configure(level.waveConfig, route);
        }

        private static void ApplyInactiveFieldFill()
        {
            Camera camera = Camera.main;
            if (camera)
            {
                // The scene used Skybox clear flags, which ignores backgroundColor and
                // exposed a different colour outside the authored Background sprite.
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = BackgroundEdgeColor;
            }

            GameObject background = GameObject.Find("Background");
            if (background && background.TryGetComponent(out SpriteRenderer renderer))
            {
                renderer.enabled = true;
                FitBackgroundToViewport(camera, renderer);
            }
        }

        private static void FitBackgroundToViewport(Camera camera, SpriteRenderer renderer)
        {
            if (!camera || !renderer.sprite) return;
            float spriteWidth = renderer.sprite.bounds.size.x;
            if (spriteWidth <= Mathf.Epsilon) return;

            Transform background = renderer.transform;
            Vector3 scale = background.localScale;
            scale.x = camera.orthographicSize * 2f * camera.aspect / spriteWidth;
            background.localScale = scale;

            Vector3 offset = camera.transform.position - renderer.bounds.center;
            background.position += new Vector3(offset.x, offset.y, 0f);
        }

        private static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
                Object.Destroy(parent.GetChild(index).gameObject);
        }

        private static Sprite FindSprite(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            return target ? target.GetComponent<SpriteRenderer>()?.sprite : null;
        }

        private static void DrawRoute(Vector2[] route, RouteTilePalette palette, Transform root)
        {
            Dictionary<Vector2Int, Sprite> tiles = new();
            for (int segment = 0; segment < route.Length - 1; segment++)
            {
                Vector2 direction = (route[segment + 1] - route[segment]).normalized;
                bool horizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);
                int count = Mathf.RoundToInt(Vector2.Distance(route[segment], route[segment + 1]) / TileStep);
                for (int step = 0; step <= count; step++)
                {
                    Vector2 position = route[segment] + direction * (step * TileStep);
                    SetTile(tiles, position, horizontal ? palette.roadCenterHorizontal : palette.roadCenterVertical);

                    if (horizontal)
                    {
                        SetSegmentBorder(tiles, route, segment, step, count, direction, Vector2.up,
                            position + Vector2.up * BorderOffset, palette.roadBorderTopHorizontal);
                        SetSegmentBorder(tiles, route, segment, step, count, direction, Vector2.down,
                            position + Vector2.down * BorderOffset, palette.roadBorderBottomHorizontal);
                    }
                    else
                    {
                        SetSegmentBorder(tiles, route, segment, step, count, direction, Vector2.left,
                            position + Vector2.left * BorderOffset, palette.roadBorderLeftVertical);
                        SetSegmentBorder(tiles, route, segment, step, count, direction, Vector2.right,
                            position + Vector2.right * BorderOffset, palette.roadBorderRightVertical);
                    }
                }
            }

            for (int point = 1; point < route.Length - 1; point++)
                SetCornerTile(tiles, route[point] - route[point - 1], route[point + 1] - route[point], route[point], palette);

            foreach ((Vector2Int key, Sprite sprite) in tiles)
                CreateTile("RouteTile", new Vector2(key.x, key.y) * .5f, sprite, root, Color.white);
        }

        private static void SetCornerTile(
            Dictionary<Vector2Int, Sprite> tiles,
            Vector2 incoming,
            Vector2 outgoing,
            Vector2 position,
            RouteTilePalette palette)
        {
            Vector2 incomingDirection = incoming.normalized;
            Vector2 outgoingDirection = outgoing.normalized;
            Vector2 outerOffset = new(
                Mathf.Sign(incomingDirection.x - outgoingDirection.x),
                Mathf.Sign(incomingDirection.y - outgoingDirection.y));
            if (outerOffset.sqrMagnitude < .5f) return;

            Vector2 innerOffset = -outerOffset;
            SetTile(tiles, position + outerOffset * BorderOffset, SelectCorner(palette, false, outerOffset));
            Vector2 innerPosition = position + innerOffset * BorderOffset;
            SetTile(tiles, innerPosition, SelectCorner(palette, true, innerOffset));
        }

        private static void SetTile(Dictionary<Vector2Int, Sprite> tiles, Vector2 position, Sprite sprite)
        {
            tiles[ToTileKey(position)] = sprite;
        }

        private static Vector2Int ToTileKey(Vector2 position) =>
            new(Mathf.RoundToInt(position.x * 2f), Mathf.RoundToInt(position.y * 2f));

        private static void SetSegmentBorder(
            Dictionary<Vector2Int, Sprite> tiles,
            Vector2[] route,
            int segment,
            int step,
            int count,
            Vector2 direction,
            Vector2 side,
            Vector2 position,
            Sprite sprite)
        {
            if (IsInnerCornerBorder(route, segment, step, count, direction, side)) return;
            if (TryGetInnerAdjacentShortening(route, segment, step, count, direction, side, out Vector2 shortening))
            {
                SetTile(tiles, position + shortening * BorderOffset, sprite);
                return;
            }
            SetTile(tiles, position, sprite);
        }

        private static bool TryGetInnerAdjacentShortening(
            Vector2[] route,
            int segment,
            int step,
            int count,
            Vector2 direction,
            Vector2 side,
            out Vector2 shortening)
        {
            shortening = Vector2.zero;
            Vector2 incoming;
            Vector2 outgoing;
            if (step == 1 && segment > 0)
            {
                incoming = (route[segment] - route[segment - 1]).normalized;
                outgoing = direction;
                shortening = direction;
            }
            else if (step == count - 1 && segment < route.Length - 2)
            {
                incoming = direction;
                outgoing = (route[segment + 2] - route[segment + 1]).normalized;
                shortening = -direction;
            }
            else
            {
                return false;
            }

            Vector2 outerOffset = new(
                Mathf.Sign(incoming.x - outgoing.x),
                Mathf.Sign(incoming.y - outgoing.y));
            bool isInnerBorder = Vector2.Dot(side, outerOffset) < 0f;
            if (!isInnerBorder) shortening = Vector2.zero;
            return isInnerBorder;
        }

        private static bool IsInnerCornerBorder(
            Vector2[] route,
            int segment,
            int step,
            int count,
            Vector2 direction,
            Vector2 side)
        {
            Vector2 incoming;
            Vector2 outgoing;
            if (step == 0 && segment > 0)
            {
                incoming = (route[segment] - route[segment - 1]).normalized;
                outgoing = direction;
            }
            else if (step == count && segment < route.Length - 2)
            {
                incoming = direction;
                outgoing = (route[segment + 2] - route[segment + 1]).normalized;
            }
            else
            {
                return false;
            }

            Vector2 outerOffset = new(
                Mathf.Sign(incoming.x - outgoing.x),
                Mathf.Sign(incoming.y - outgoing.y));
            return Vector2.Dot(side, outerOffset) < 0f;
        }

        private static Sprite SelectCorner(RouteTilePalette palette, bool inner, Vector2 direction)
        {
            bool top = direction.y > 0f;
            bool left = direction.x < 0f;
            if (inner)
            {
                if (top) return left ? palette.roadBorderInnerCornerTopLeft : palette.roadBorderInnerCornerTopRight;
                return left ? palette.roadBorderInnerCornerBottomLeft : palette.roadBorderInnerCornerBottomRight;
            }

            if (top) return left ? palette.roadBorderOuterCornerTopLeft : palette.roadBorderOuterCornerTopRight;
            return left ? palette.roadBorderOuterCornerBottomLeft : palette.roadBorderOuterCornerBottomRight;
        }

        private static bool HasCompletePalette(RouteTilePalette palette)
        {
            return palette != null && palette.roadCenterHorizontal && palette.roadCenterVertical &&
                   palette.roadBorderTopHorizontal && palette.roadBorderBottomHorizontal &&
                   palette.roadBorderLeftVertical && palette.roadBorderRightVertical &&
                   palette.roadBorderInnerCornerTopLeft && palette.roadBorderInnerCornerTopRight &&
                   palette.roadBorderInnerCornerBottomLeft && palette.roadBorderInnerCornerBottomRight &&
                   palette.roadBorderOuterCornerTopLeft && palette.roadBorderOuterCornerTopRight &&
                   palette.roadBorderOuterCornerBottomLeft && palette.roadBorderOuterCornerBottomRight;
        }

        private static void CreateTile(string name, Vector2 position, Sprite sprite, Transform root, Color color)
        {
            GameObject tile = new(name);
            tile.transform.SetParent(root);
            tile.transform.position = position;
            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = -2;
        }

    }
}
