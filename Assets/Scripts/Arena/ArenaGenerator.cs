// =============================================================================
// ArenaGenerator.cs
// =============================================================================
// Generates a runtime arena (floor, boundary walls, random obstacles) for the
// wave-based Diablo-style action game.  It also bakes a NavMesh so enemies can
// path-find immediately after generation.
//
// QUICK SETUP IN UNITY
// --------------------
// 1. Create an empty GameObject in your scene and add this component.
// 2. (Optional) Create a floor Quad/Plane prefab with your MeshyAI texture and
//    assign it to the "Floor Prefab" slot.
// 3. (Optional) Create obstacle prefabs and assign them to "Obstacle Prefabs".
// 4. (Optional) Create a wall prefab and assign it to "Wall Prefab".
// 5. Press Play — the arena generates and NavMesh bakes automatically.
//
// NAVMESH REQUIREMENT
// -------------------
// NavMesh baking uses Unity's "AI Navigation" package (com.unity.ai.navigation).
// This package is already listed in Packages/manifest.json for this project.
// If baking fails, open Window > Package Manager and ensure "AI Navigation" is
// installed (version 2.x).
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

/// <summary>
/// Handles runtime arena setup: spawns a floor, four boundary walls, and a
/// configurable number of randomly-placed obstacles, then bakes a NavMesh so
/// enemies can navigate immediately.
/// </summary>
public class ArenaGenerator : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields — Arena dimensions
    // -------------------------------------------------------------------------

    [Header("Arena Dimensions")]

    /// <summary>Arena width along the X axis (units).</summary>
    [Tooltip("Arena width along the X axis in world units.")]
    public float arenaWidth = 40f;

    /// <summary>Arena depth along the Z axis (units).</summary>
    [Tooltip("Arena depth along the Z axis in world units.")]
    public float arenaLength = 40f;

    // -------------------------------------------------------------------------
    // Inspector fields — Prefabs (all optional)
    // -------------------------------------------------------------------------

    [Header("Prefabs (all optional — primitives used as fallback)")]

    /// <summary>
    /// Optional floor prefab (e.g. a Quad with your MeshyAI texture).
    /// When null the script creates a default white Quad at runtime.
    /// </summary>
    [Tooltip("Textured floor prefab (Quad or Plane). Leave null to use a plain white placeholder.")]
    public GameObject floorPrefab;

    /// <summary>
    /// Optional wall prefab.  When null the script creates invisible box
    /// colliders so boundaries still block movement without any visual mesh.
    /// </summary>
    [Tooltip("Wall prefab to use at each boundary edge. Leave null for invisible collider walls.")]
    public GameObject wallPrefab;

    /// <summary>
    /// Array of obstacle prefabs to randomly pick from.
    /// When empty the script places gray placeholder cubes.
    /// </summary>
    [Tooltip("Obstacle prefabs to scatter around the arena. Leave empty for gray placeholder cubes.")]
    public GameObject[] obstaclePrefabs;

    // -------------------------------------------------------------------------
    // Inspector fields — Obstacle settings
    // -------------------------------------------------------------------------

    [Header("Obstacle Settings")]

    /// <summary>Total number of obstacles to attempt to place.</summary>
    [Tooltip("How many obstacles to scatter around the arena.")]
    [Range(0, 50)]
    public int obstacleCount = 12;

    /// <summary>Minimum distance between any two obstacle centres (units).</summary>
    [Tooltip("Minimum world-space distance required between any two placed obstacles.")]
    public float minObstacleSpacing = 3f;

    /// <summary>
    /// Radius around (0,0,0) that is kept clear of obstacles so the player can
    /// safely spawn at the arena centre.
    /// </summary>
    [Tooltip("Radius around the arena centre (player spawn) kept free of obstacles.")]
    public float playerSafeRadius = 6f;

    // -------------------------------------------------------------------------
    // Inspector fields — Wall geometry
    // -------------------------------------------------------------------------

    [Header("Wall Geometry")]

    /// <summary>Height of boundary walls (units).</summary>
    [Tooltip("Height of boundary walls in world units.")]
    public float wallHeight = 4f;

    /// <summary>Thickness of boundary walls (units).</summary>
    [Tooltip("Thickness of boundary walls in world units.")]
    public float wallThickness = 1f;

    // -------------------------------------------------------------------------
    // Inspector fields — Generation control
    // -------------------------------------------------------------------------

    [Header("Generation Control")]

    /// <summary>
    /// Fixed seed for reproducible layouts.  Set to -1 to use a random seed
    /// each time Play is pressed.
    /// </summary>
    [Tooltip("Fixed seed for reproducible arena layouts. Use -1 for a random seed each run.")]
    public int randomSeed = -1;

    /// <summary>
    /// When true, bakes a NavMesh on the floor after all objects are placed so
    /// enemies can path-find immediately.  Requires the AI Navigation package.
    /// </summary>
    [Tooltip("Bake a runtime NavMesh after generation so enemies can navigate. Requires the AI Navigation package.")]
    public bool bakeNavMesh = true;

    // -------------------------------------------------------------------------
    // Private constants
    // -------------------------------------------------------------------------

    /// <summary>Y-thickness of the floor BoxCollider in local space (before scale).</summary>
    private const float FloorColliderThickness = 0.1f;

    /// <summary>Guard against division-by-zero when computing collider size from scale.</summary>
    private const float MinScaleGuard = 0.001f;

    /// <summary>Margin from each wall kept clear when randomly positioning obstacles.</summary>
    private const float ObstacleWallMargin = 2f;

    /// <summary>Maximum placement attempts per obstacle before skipping it.</summary>
    private const int MaxObstaclePlacementAttempts = 30;

    /// <summary>Width and depth of the placeholder cube obstacle.</summary>
    private const float PlaceholderObstacleWidth = 1.5f;

    /// <summary>Height of the placeholder cube obstacle.</summary>
    private const float PlaceholderObstacleHeight = 2f;

    /// <summary>Radius of each obstacle's gizmo sphere drawn in the scene view.</summary>
    private const float ObstacleGizmoRadius = 0.4f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private Transform _floorContainer;
    private Transform _wallsContainer;
    private Transform _obstaclesContainer;

    private readonly List<Vector3> _placedObstaclePositions = new List<Vector3>();

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        GenerateArena();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Destroys all previously generated children, then generates a fresh arena
    /// (floor, walls, obstacles) and optionally bakes a NavMesh.
    /// Safe to call multiple times — e.g. between waves to regenerate the layout.
    /// </summary>
    public void GenerateArena()
    {
        ClearArena();

        // Seed the RNG before any placement decisions.
        int seed = randomSeed >= 0 ? randomSeed : Random.Range(0, int.MaxValue);
        Random.InitState(seed);
        Debug.Log($"[ArenaGenerator] Generating arena {arenaWidth}×{arenaLength} with seed {seed}.");

        // Create hierarchy containers.
        _floorContainer     = CreateContainer("Floor");
        _wallsContainer     = CreateContainer("Walls");
        _obstaclesContainer = CreateContainer("Obstacles");

        SpawnFloor();
        SpawnWalls();
        SpawnObstacles();

        if (bakeNavMesh)
            BakeNavMesh();

        Debug.Log($"[ArenaGenerator] Generation complete. " +
                  $"Obstacles placed: {_placedObstaclePositions.Count}/{obstacleCount}. " +
                  $"NavMesh bake: {(bakeNavMesh ? "requested" : "skipped")}.");
    }

    /// <summary>
    /// Destroys all child GameObjects that were created by a previous call to
    /// <see cref="GenerateArena"/>, resetting the arena to an empty state.
    /// </summary>
    public void ClearArena()
    {
        // Destroy all children of this GameObject (the generated arena objects).
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        _placedObstaclePositions.Clear();
        _floorContainer     = null;
        _wallsContainer     = null;
        _obstaclesContainer = null;
    }

    // -------------------------------------------------------------------------
    // Floor
    // -------------------------------------------------------------------------

    private void SpawnFloor()
    {
        GameObject floor;

        if (floorPrefab != null)
        {
            floor = Instantiate(floorPrefab, Vector3.zero, Quaternion.identity, _floorContainer);
            floor.name = "Floor";
            // Scale the prefab to match the requested arena dimensions.
            floor.transform.localScale = new Vector3(arenaWidth, arenaLength, 1f);
        }
        else
        {
            // Placeholder: white Quad rotated to lie flat.
            floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name = "Floor_Placeholder";
            floor.transform.SetParent(_floorContainer, false);
            floor.transform.position = Vector3.zero;
            floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.localScale = new Vector3(arenaWidth, arenaLength, 1f);

            // Apply a plain white material so the floor is clearly visible.
            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                                 ?? Shader.Find("Standard"))
                {
                    color = Color.white
                };
            }
        }

        // Ensure there is a thin BoxCollider so the NavMesh surface has geometry to bake on.
        if (floor.GetComponent<BoxCollider>() == null)
        {
            var col = floor.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, FloorColliderThickness / Mathf.Max(floor.transform.localScale.y, MinScaleGuard), 1f);
        }

        // Tag floor so NavMesh can identify it.
        floor.tag = "Untagged"; // keep default; NavMeshSurface is added below.
        SetNavigationStatic(floor);

        // Attach the NavMeshSurface to the floor object — BuildNavMesh() is
        // called later in BakeNavMesh() once all obstacles are in place.
        if (bakeNavMesh && floor.GetComponent<NavMeshSurface>() == null)
            floor.AddComponent<NavMeshSurface>();
    }

    // -------------------------------------------------------------------------
    // Walls
    // -------------------------------------------------------------------------

    private void SpawnWalls()
    {
        // Four walls: +X, -X, +Z, -Z
        // Each wall spans the full length of the opposite dimension.
        CreateWall("Wall_PosX",
            new Vector3( arenaWidth * 0.5f + wallThickness * 0.5f, wallHeight * 0.5f, 0f),
            new Vector3(wallThickness, wallHeight, arenaLength + wallThickness * 2f));

        CreateWall("Wall_NegX",
            new Vector3(-arenaWidth * 0.5f - wallThickness * 0.5f, wallHeight * 0.5f, 0f),
            new Vector3(wallThickness, wallHeight, arenaLength + wallThickness * 2f));

        CreateWall("Wall_PosZ",
            new Vector3(0f, wallHeight * 0.5f,  arenaLength * 0.5f + wallThickness * 0.5f),
            new Vector3(arenaWidth + wallThickness * 2f, wallHeight, wallThickness));

        CreateWall("Wall_NegZ",
            new Vector3(0f, wallHeight * 0.5f, -arenaLength * 0.5f - wallThickness * 0.5f),
            new Vector3(arenaWidth + wallThickness * 2f, wallHeight, wallThickness));
    }

    private void CreateWall(string wallName, Vector3 position, Vector3 size)
    {
        if (wallPrefab != null)
        {
            var wall = Instantiate(wallPrefab, position, Quaternion.identity, _wallsContainer);
            wall.name = wallName;
            wall.transform.localScale = size;
        }
        else
        {
            // Invisible wall: just a BoxCollider, no renderer.
            var wallGO = new GameObject(wallName + "_Collider");
            wallGO.transform.SetParent(_wallsContainer, false);
            wallGO.transform.position = position;

            var col = wallGO.AddComponent<BoxCollider>();
            col.size = size;

            SetNavigationStatic(wallGO);
        }
    }

    // -------------------------------------------------------------------------
    // Obstacles
    // -------------------------------------------------------------------------

    private void SpawnObstacles()
    {
        const int maxAttempts = MaxObstaclePlacementAttempts;

        // Margins so obstacles don't clip into walls.
        float halfW = arenaWidth  * 0.5f - ObstacleWallMargin;
        float halfL = arenaLength * 0.5f - ObstacleWallMargin;

        for (int i = 0; i < obstacleCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(-halfW, halfW),
                    0f,
                    Random.Range(-halfL, halfL));

                // Reject positions inside the player's safe zone.
                if (candidate.magnitude < playerSafeRadius)
                    continue;

                // Reject positions that are too close to already-placed obstacles.
                bool tooClose = false;
                foreach (var pos in _placedObstaclePositions)
                {
                    if (Vector3.Distance(candidate, pos) < minObstacleSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // Valid position found — instantiate obstacle.
                PlaceObstacle(candidate, i);
                _placedObstaclePositions.Add(candidate);
                placed = true;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning($"[ArenaGenerator] Could not place obstacle {i} after {maxAttempts} attempts — skipped.");
            }
        }
    }

    private void PlaceObstacle(Vector3 position, int index)
    {
        GameObject obstacle;
        bool isPlaceholder;

        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            var prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            obstacle = Instantiate(prefab, position, Quaternion.identity, _obstaclesContainer);
            obstacle.name = $"Obstacle_{index}";
            isPlaceholder = false;
        }
        else
        {
            // Placeholder: gray cube ~1.5 wide, ~2 tall.
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Obstacle_Placeholder_{index}";
            obstacle.transform.SetParent(_obstaclesContainer, false);
            obstacle.transform.position = position;
            obstacle.transform.localScale = new Vector3(PlaceholderObstacleWidth, PlaceholderObstacleHeight, PlaceholderObstacleWidth);

            var renderer = obstacle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                                 ?? Shader.Find("Standard"))
                {
                    color = new Color(0.5f, 0.5f, 0.5f)
                };
            }
            isPlaceholder = true;
        }

        // Random Y rotation for visual variety.
        obstacle.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Mark as navigation-static and tag it so other systems can find it.
        SetNavigationStatic(obstacle);

        var data = obstacle.GetComponent<ObstacleData>() ?? obstacle.AddComponent<ObstacleData>();
        data.isPlaceholder = isPlaceholder;
    }

    // -------------------------------------------------------------------------
    // NavMesh baking
    // -------------------------------------------------------------------------

    private void BakeNavMesh()
    {
        // Find the NavMeshSurface we attached to the floor in SpawnFloor().
        var surface = _floorContainer != null
            ? _floorContainer.GetComponentInChildren<NavMeshSurface>()
            : GetComponentInChildren<NavMeshSurface>();

        if (surface == null)
        {
            Debug.LogWarning("[ArenaGenerator] bakeNavMesh is true but no NavMeshSurface was found. " +
                             "Ensure the AI Navigation package (com.unity.ai.navigation) is installed.");
            return;
        }

        surface.BuildNavMesh();
        Debug.Log("[ArenaGenerator] NavMesh baked successfully.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private Transform CreateContainer(string containerName)
    {
        var go = new GameObject(containerName);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    /// <summary>
    /// Marks a GameObject as Navigation Static so the baked NavMesh treats it
    /// as walkable surface or obstacle geometry.
    /// </summary>
    private static void SetNavigationStatic(GameObject go)
    {
        go.isStatic = true;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        // Arena bounds — yellow wire rectangle on the XZ plane.
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        DrawXZRect(center, arenaWidth, arenaLength);

        // Player safe zone — green wire sphere at arena centre.
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, playerSafeRadius);

        // Previously placed obstacles — small red wire spheres.
        Gizmos.color = Color.red;
        foreach (var pos in _placedObstaclePositions)
            Gizmos.DrawWireSphere(pos, ObstacleGizmoRadius);
    }

    private static void DrawXZRect(Vector3 center, float width, float length)
    {
        float hw = width  * 0.5f;
        float hl = length * 0.5f;

        Vector3 c0 = center + new Vector3(-hw, 0f, -hl);
        Vector3 c1 = center + new Vector3( hw, 0f, -hl);
        Vector3 c2 = center + new Vector3( hw, 0f,  hl);
        Vector3 c3 = center + new Vector3(-hw, 0f,  hl);

        Gizmos.DrawLine(c0, c1);
        Gizmos.DrawLine(c1, c2);
        Gizmos.DrawLine(c2, c3);
        Gizmos.DrawLine(c3, c0);
    }
}
