using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Renders a tube-like mesh along a path of positions with various options for radius control.
/// </summary>
[ExecuteInEditMode]
public class TubeRenderer : MonoBehaviour
{
    [Tooltip("Array of points defining the tube's path")]
    [SerializeField] private Vector3[] positions;
    
    [Tooltip("Number of sides around the tube circumference")]
    [SerializeField] private int sides = 8;

    [Header("Radius Control")]
    [Tooltip("How the tube's radius is determined along its length")]
    [SerializeField] private RadiusMode radiusMode = RadiusMode.Single;
    
    [Tooltip("Animation curve controlling the radius when using Curve mode")]
    [SerializeField] private AnimationCurve radiusCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    
    [Tooltip("Base radius value")]
    [SerializeField] private float radiusOne = 1.0f;
    
    [Tooltip("End radius value (used only in StartEnd mode)")]
    [SerializeField] private float radiusTwo = 1.0f;
    
    /// <summary>
    /// How the tube's radius is determined along its length
    /// </summary>
    private enum RadiusMode 
    { 
        /// <summary>Use a single radius value for the entire tube</summary>
        Single, 
        
        /// <summary>Interpolate between two radius values from start to end</summary>
        StartEnd, 
        
        /// <summary>Use an animation curve to define radius along the tube</summary>
        Curve 
    }
    
    private Vector3[] _vertices;
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private bool _meshNeedsRebuild = true;

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
    public Vector3[] Positions
    {
        get { return positions; }
        set 
        { 
            positions = value;
            _meshNeedsRebuild = true;
        }
    }

    private void Awake()
    {
        InitializeComponents();
    }

    private void Reset()
    {
        // Set default positions when component is first added
        positions = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1)
        };
    }

    private void OnEnable()
    {
        _meshRenderer.enabled = true;
    }

    private void OnDisable()
    {
        _meshRenderer.enabled = false;
    }
    
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

    private void Update()
    {
        GenerateMesh();
    }

    private void OnValidate()
    {
        sides = Mathf.Max(3, sides);
        _meshNeedsRebuild = true;
    }

    /// <summary>
    /// Set new positions for the tube and regenerate the mesh
    /// </summary>
    /// <param name="newPositions">Array of Vector3 positions defining the tube's path</param>
    public void SetPositions(Vector3[] newPositions)
    {
        this.positions = newPositions;
        _meshNeedsRebuild = true;
        GenerateMesh();
    }
    
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

    private void GenerateMesh()
    {
        // If we don't have enough points to create a tube
        if (_mesh == null || positions == null || positions.Length <= 1)
        {
            if (_mesh != null)
            {
                _mesh.Clear();
            }
            return;
        }
        
        // Calculate the vertices length based on positions and sides
        var verticesLength = sides * positions.Length;
        
        // Check if we need to rebuild arrays
        if (_vertices == null || _vertices.Length != verticesLength || _meshNeedsRebuild)
        {
            _vertices = new Vector3[verticesLength];
            
            var indices = GenerateIndices(positions.Length);
            var uvs = GenerateUVs(positions.Length);
            
            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.triangles = indices;
            _mesh.uv = uvs;
            
            _meshNeedsRebuild = false;
        }

        // Generate the mesh vertices
        var currentVertIndex = 0;
        for (int i = 0; i < positions.Length; i++)
        {
            var circle = CalculateCircle(i);
            foreach (var vertex in circle)
            {
                _vertices[currentVertIndex++] = vertex;
            }
        }

        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _meshFilter.mesh = _mesh;
    }

    private Vector2[] GenerateUVs(int positionCount)
    {
        var uvs = new Vector2[positionCount * sides];

        for (int segment = 0; segment < positionCount; segment++)
        {
            for (int side = 0; side < sides; side++)
            {
                var vertIndex = (segment * sides + side);
                var u = side / (sides - 1f);
                var v = segment / (positionCount - 1f);

                uvs[vertIndex] = new Vector2(u, v);
            }
        }

        return uvs;
    }

    private int[] GenerateIndices(int positionCount)
    {
        // Two triangles and 3 vertices
        var indices = new int[(positionCount - 1) * sides * 2 * 3];

        var currentIndicesIndex = 0;
        for (int segment = 1; segment < positionCount; segment++)
        {
            for (int side = 0; side < sides; side++)
            {
                var vertIndex = (segment * sides + side);
                var prevVertIndex = vertIndex - sides;

                // Triangle one
                indices[currentIndicesIndex++] = prevVertIndex;
                indices[currentIndicesIndex++] = (side == sides - 1) ? (vertIndex - (sides - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = vertIndex;

                // Triangle two
                indices[currentIndicesIndex++] = (side == sides - 1) ? (prevVertIndex - (sides - 1)) : (prevVertIndex + 1);
                indices[currentIndicesIndex++] = (side == sides - 1) ? (vertIndex - (sides - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = prevVertIndex;
            }
        }

        return indices;
    }

    private Vector3[] CalculateCircle(int index)
    {
        var dirCount = 0;
        var forward = Vector3.zero;

        // If not first index
        if (index > 0)
        {
            forward += (positions[index] - positions[index - 1]).normalized;
            dirCount++;
        }

        // If not last index
        if (index < positions.Length - 1)
        {
            forward += (positions[index + 1] - positions[index]).normalized;
            dirCount++;
        }

        // Forward is the average of the connecting edges directions
        forward = (forward / dirCount).normalized;
        var side = Vector3.Cross(forward, forward + new Vector3(.123564f, .34675f, .756892f)).normalized;
        var up = Vector3.Cross(forward, side).normalized;

        var circle = new Vector3[sides];
        var angle = 0f;
        var angleStep = (2 * Mathf.PI) / sides;

        var t = index / (positions.Length - 1f);
        float radius;
        
        // Calculate radius based on the selected mode
        switch (radiusMode)
        {
            case RadiusMode.StartEnd:
                radius = Mathf.Lerp(radiusOne, radiusTwo, t);
                break;
            case RadiusMode.Curve:
                radius = radiusCurve.Evaluate(t);
                break;
            case RadiusMode.Single:
            default:
                radius = radiusOne;
                break;
        }

        for (int i = 0; i < sides; i++)
        {
            var x = Mathf.Cos(angle);
            var y = Mathf.Sin(angle);

            circle[i] = positions[index] + side * x * radius + up * y * radius;

            angle += angleStep;
        }

        return circle;
    }
}