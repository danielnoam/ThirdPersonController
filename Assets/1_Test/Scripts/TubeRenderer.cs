using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// TubeRenderer generates a tube-like mesh along a path of positions.
/// Enhanced version with memory management, performance optimizations, and additional features.
/// </summary>
[ExecuteInEditMode]
public class TubeRenderer : MonoBehaviour
{
    /// <summary>
    /// Defines how the radius of the tube is determined
    /// </summary>
    public enum RadiusMode
    {
        /// <summary>Use a single radius value for the entire tube</summary>
        Single,
        /// <summary>Interpolate between two radius values from start to end</summary>
        StartEnd,
        /// <summary>Use an animation curve to define radius along the tube</summary>
        Curve
    }

    [Header("Path Settings")]
    [SerializeField] private Vector3[] _positions;
    [SerializeField] private bool _useWorldSpace = true;
    [SerializeField] private bool _closeTube = false;

    [Header("Shape Settings")]
    [SerializeField] private int _sides = 8;
    [SerializeField] private RadiusMode _radiusMode = RadiusMode.Single;
    [SerializeField] private float _radiusOne = 1.0f;
    [SerializeField] private float _radiusTwo = 1.0f;
    [SerializeField] private AnimationCurve _radiusCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Performance Settings")]
    [SerializeField] private bool _generateOnUpdate = false;
    [SerializeField] private bool _optimizeChanges = true;

    // Cached values for optimization
    private Vector3[] _vertices;
    private int[] _indices;
    private Vector2[] _uvs;
    private Vector3[] _lastPositions;
    private bool _meshNeedsRebuild = true;
    private int _lastSides = 0;
    private RadiusMode _lastRadiusMode = RadiusMode.Single;
    private float _lastRadiusOne = 0f;
    private float _lastRadiusTwo = 0f;
    private List<Vector3> _circleVertices = new List<Vector3>();

    // Mesh components
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    /// <summary>
    /// Get or set the material used for the tube renderer
    /// </summary>
    public Material material
    {
        get { return _meshRenderer.material; }
        set { _meshRenderer.material = value; }
    }

    /// <summary>
    /// Get or set the positions that define the tube's path
    /// </summary>
    public Vector3[] positions
    {
        get { return _positions; }
        set 
        { 
            _positions = value;
            _meshNeedsRebuild = true;
        }
    }

    /// <summary>
    /// Initialize components on awake
    /// </summary>
    private void Awake()
    {
        InitializeComponents();
    }

    /// <summary>
    /// Enable the mesh renderer when the component is enabled
    /// </summary>
    private void OnEnable()
    {
        _meshRenderer.enabled = true;
        _meshNeedsRebuild = true;
    }

    /// <summary>
    /// Disable the mesh renderer when the component is disabled
    /// </summary>
    private void OnDisable()
    {
        _meshRenderer.enabled = false;
    }

    /// <summary>
    /// Clean up resources when the object is destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (_mesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_mesh);
            }
            else
            {
                DestroyImmediate(_mesh);
            }
            _mesh = null;
        }
    }

    /// <summary>
    /// Update the mesh if using world space or if specifically requested
    /// </summary>
    private void Update()
    {
        if (_generateOnUpdate || (_useWorldSpace && CheckPositionsChanged()))
        {
            GenerateMesh();
        }
    }

    /// <summary>
    /// Ensure parameters are valid when changed in the inspector
    /// </summary>
    private void OnValidate()
    {
        _sides = Mathf.Max(3, _sides);
        
        // Check if key parameters have changed
        if (_lastSides != _sides || 
            _lastRadiusMode != _radiusMode || 
            _lastRadiusOne != _radiusOne || 
            _lastRadiusTwo != _radiusTwo)
        {
            _meshNeedsRebuild = true;
            _lastSides = _sides;
            _lastRadiusMode = _radiusMode;
            _lastRadiusOne = _radiusOne;
            _lastRadiusTwo = _radiusTwo;
        }
    }

    /// <summary>
    /// Set new positions for the tube and regenerate the mesh
    /// </summary>
    /// <param name="positions">Array of Vector3 positions defining the tube's path</param>
    public void SetPositions(Vector3[] positions)
    {
        _positions = positions;
        _meshNeedsRebuild = true;
        GenerateMesh();
    }

    /// <summary>
    /// Initialize the required components for the tube renderer
    /// </summary>
    private void InitializeComponents()
    {
        _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter == null)
        {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "TubeMesh";
            _meshFilter.mesh = _mesh;
        }
    }

    /// <summary>
    /// Check if positions have changed since last update
    /// </summary>
    /// <returns>True if positions have changed, false otherwise</returns>
    private bool CheckPositionsChanged()
    {
        if (_positions == null || _lastPositions == null || 
            _positions.Length != _lastPositions.Length)
        {
            // Cache the current positions
            if (_positions != null)
            {
                _lastPositions = new Vector3[_positions.Length];
                System.Array.Copy(_positions, _lastPositions, _positions.Length);
            }
            return true;
        }

        // Check if any position has changed
        for (int i = 0; i < _positions.Length; i++)
        {
            if (_positions[i] != _lastPositions[i])
            {
                // Update the cached positions
                System.Array.Copy(_positions, _lastPositions, _positions.Length);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Generate the tube mesh based on the current settings
    /// </summary>
    private void GenerateMesh()
    {
        if (_positions == null || _positions.Length <= 1)
        {
            if (_mesh != null) 
            {
                _mesh.Clear();
            }
            return;
        }

        // Calculate the effective number of positions (including duplication for closed tube)
        int effectivePositionCount = _positions.Length;
        if (_closeTube && _positions.Length > 2)
        {
            effectivePositionCount += 1; // Add one more for closing the loop
        }

        // Calculate vertices count
        int verticesLength = _sides * effectivePositionCount;

        // Allocate or resize arrays as needed
        if (_vertices == null || _vertices.Length != verticesLength || _meshNeedsRebuild)
        {
            _vertices = new Vector3[verticesLength];
            _indices = GenerateIndices(effectivePositionCount);
            _uvs = GenerateUVs(effectivePositionCount);
            
            _meshNeedsRebuild = false;

            // Clear the mesh before setting new values
            _mesh.Clear();
        }

        // Generate vertices
        int currentVertIndex = 0;
        for (int i = 0; i < effectivePositionCount; i++)
        {
            // Handle the closing position for closed tubes
            int posIndex = i;
            if (_closeTube && i == effectivePositionCount - 1)
            {
                posIndex = 0; // Use the first position to close the loop
            }
            else
            {
                posIndex = i;
            }

            CalculateCircle(posIndex, effectivePositionCount, _circleVertices);
            foreach (var vertex in _circleVertices)
            {
                _vertices[currentVertIndex++] = _useWorldSpace ? transform.InverseTransformPoint(vertex) : vertex;
            }
        }

        // Apply the mesh data
        _mesh.vertices = _vertices;
        if (_meshNeedsRebuild)
        {
            _mesh.triangles = _indices;
            _mesh.uv = _uvs;
        }
        
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _meshFilter.mesh = _mesh;
    }

    /// <summary>
    /// Generate UV coordinates for the tube mesh
    /// </summary>
    /// <param name="positionCount">Number of positions in the tube</param>
    /// <returns>Array of UV coordinates</returns>
    private Vector2[] GenerateUVs(int positionCount)
    {
        var uvs = new Vector2[positionCount * _sides];

        for (int segment = 0; segment < positionCount; segment++)
        {
            for (int side = 0; side < _sides; side++)
            {
                var vertIndex = (segment * _sides + side);
                var u = side / (_sides - 1f);
                var v = segment / (positionCount - 1f);

                uvs[vertIndex] = new Vector2(u, v);
            }
        }

        return uvs;
    }

    /// <summary>
    /// Generate triangle indices for the tube mesh
    /// </summary>
    /// <param name="positionCount">Number of positions in the tube</param>
    /// <returns>Array of triangle indices</returns>
    private int[] GenerateIndices(int positionCount)
    {
        // Two triangles per quad, 3 indices per triangle
        var indices = new int[(positionCount - 1) * _sides * 2 * 3];

        var currentIndicesIndex = 0;
        for (int segment = 1; segment < positionCount; segment++)
        {
            for (int side = 0; side < _sides; side++)
            {
                var vertIndex = (segment * _sides + side);
                var prevVertIndex = vertIndex - _sides;

                // Triangle one
                indices[currentIndicesIndex++] = prevVertIndex;
                indices[currentIndicesIndex++] = (side == _sides - 1) ? (vertIndex - (_sides - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = vertIndex;

                // Triangle two
                indices[currentIndicesIndex++] = (side == _sides - 1) ? (prevVertIndex - (_sides - 1)) : (prevVertIndex + 1);
                indices[currentIndicesIndex++] = (side == _sides - 1) ? (vertIndex - (_sides - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = prevVertIndex;
            }
        }

        return indices;
    }

    /// <summary>
    /// Calculate a circle of vertices at the specified position index
    /// </summary>
    /// <param name="index">Position index</param>
    /// <param name="totalPositions">Total number of positions</param>
    /// <param name="circleVertices">List to store the calculated vertices</param>
    private void CalculateCircle(int index, int totalPositions, List<Vector3> circleVertices)
    {
        circleVertices.Clear();

        var dirCount = 0;
        var forward = Vector3.zero;

        // Calculate forward direction from neighboring positions
        if (_closeTube && _positions.Length > 2)
        {
            // For closed tube, handle wrapping around the ends
            int prevIndex = (index > 0) ? index - 1 : _positions.Length - 1;
            int nextIndex = (index < _positions.Length - 1) ? index + 1 : 0;

            forward += (_positions[index] - _positions[prevIndex]).normalized;
            forward += (_positions[nextIndex] - _positions[index]).normalized;
            dirCount = 2;
        }
        else
        {
            // For open tube
            if (index > 0)
            {
                forward += (_positions[index] - _positions[index - 1]).normalized;
                dirCount++;
            }

            if (index < _positions.Length - 1)
            {
                forward += (_positions[index + 1] - _positions[index]).normalized;
                dirCount++;
            }
        }

        // If we have a valid direction
        if (dirCount > 0)
        {
            // Forward is the average of the connecting edges directions
            forward = (forward / dirCount).normalized;
        }
        else
        {
            // Fallback if we can't determine a direction (shouldn't happen with valid input)
            forward = Vector3.forward;
        }

        // Create a stable coordinate frame using a consistent secondary vector
        var side = Vector3.Cross(forward, forward + new Vector3(.123564f, .34675f, .756892f)).normalized;
        var up = Vector3.Cross(forward, side).normalized;

        // Calculate radius based on the selected mode
        float t = (totalPositions <= 1) ? 0 : (float)index / (totalPositions - 1);
        float radius;

        switch (_radiusMode)
        {
            case RadiusMode.StartEnd:
                radius = Mathf.Lerp(_radiusOne, _radiusTwo, t);
                break;
            case RadiusMode.Curve:
                radius = _radiusCurve.Evaluate(t) * _radiusOne;
                break;
            case RadiusMode.Single:
            default:
                radius = _radiusOne;
                break;
        }

        // Generate the circle vertices
        float angleStep = (2 * Mathf.PI) / _sides;
        float angle = 0f;

        for (int i = 0; i < _sides; i++)
        {
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            var vertexPosition = _positions[index] + side * x * radius + up * y * radius;
            circleVertices.Add(vertexPosition);

            angle += angleStep;
        }
    }
}