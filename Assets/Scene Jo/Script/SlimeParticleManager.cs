using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages slime particles using Position Based Fluids (PBF) with cohesion and surface tension
/// </summary>
public class SlimeParticleManager : MonoBehaviour
{
    [Header("Particle Settings")]
    [Range(100, 2000)]
    public int maxParticles = 800;
    [Range(0.03f, 0.15f)]
    public float particleRadius = 0.05f;
    public float restDensity = 1.0f;
    
    [Header("Physics Settings")]
    [Range(1, 10)]
    public int solverIterations = 4;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    [Range(0.01f, 0.5f)]
    public float cohesionStrength = 0.03f;
    [Range(0.0f, 0.3f)]
    public float surfaceTension = 0.1f;
    [Range(0.1f, 0.6f)]
    public float viscosity = 0.3f;
    [Range(1, 4)]
    public int substeps = 2;
    
    [Header("Collision Settings")]
    public LayerMask collisionMask = -1;
    public float friction = 0.1f;
    public float damping = 0.99f;
    
    [Header("Initial Setup")]
    public Vector3 initialSpawnCenter = Vector3.zero;
    public float initialSpawnRadius = 0.5f;
    
    [Header("Debug")]
    public bool drawDebugSpheres = true;
    public Color debugColor = Color.green;
    
    // Internal data
    private List<Particle> particles = new List<Particle>();
    private SpatialHash spatialHash;
    private float timeStep;
    
    // Particle structure
    private class Particle
    {
        public Vector3 position;
        public Vector3 prevPosition;
        public Vector3 velocity;
        public float invMass = 1.0f;
        public float density;
        public float lambda; // Lagrange multiplier
        public List<int> neighbors = new List<int>();
    }
    
    void Start()
    {
        InitializeParticles();
        spatialHash = new SpatialHash(particleRadius * 2.0f);
    }
    
    void InitializeParticles()
    {
        particles.Clear();
        
        // Calculate grid dimensions for initial particle placement
        int particlesPerSide = Mathf.CeilToInt(Mathf.Pow(maxParticles, 1.0f / 3.0f));
        float spacing = particleRadius * 2.0f;
        Vector3 offset = initialSpawnCenter - new Vector3(
            (particlesPerSide - 1) * spacing * 0.5f,
            (particlesPerSide - 1) * spacing * 0.5f,
            (particlesPerSide - 1) * spacing * 0.5f
        );
        
        int count = 0;
        for (int x = 0; x < particlesPerSide && count < maxParticles; x++)
        {
            for (int y = 0; y < particlesPerSide && count < maxParticles; y++)
            {
                for (int z = 0; z < particlesPerSide && count < maxParticles; z++)
                {
                    Vector3 pos = offset + new Vector3(x * spacing, y * spacing, z * spacing);
                    
                    // Only spawn within initial radius
                    if (Vector3.Distance(pos, initialSpawnCenter) <= initialSpawnRadius)
                    {
                        Particle p = new Particle
                        {
                            position = pos,
                            prevPosition = pos,
                            velocity = Vector3.zero,
                            invMass = 1.0f
                        };
                        particles.Add(p);
                        count++;
                    }
                }
            }
        }
        
        Debug.Log($"Initialized {particles.Count} slime particles");
    }
    
    void FixedUpdate()
    {
        timeStep = Time.fixedDeltaTime / substeps;
        
        for (int substep = 0; substep < substeps; substep++)
        {
            SimulationStep();
        }
    }
    
    void SimulationStep()
    {
        // 1. Apply external forces and predict positions
        Integrate();
        
        // 2. Build spatial hash and find neighbors
        spatialHash.Clear();
        foreach (var p in particles)
        {
            spatialHash.Add(p.position, particles.IndexOf(p));
        }
        FindNeighbors();
        
        // 3. Solve constraints
        for (int iter = 0; iter < solverIterations; iter++)
        {
            SolveConstraints();
        }
        
        // 4. Update velocities
        UpdateVelocities();
        
        // 5. Apply viscosity
        ApplyViscosity();
        
        // 6. Handle collisions
        HandleCollisions();
    }
    
    void Integrate()
    {
        foreach (var p in particles)
        {
            // Semi-implicit Euler integration
            p.velocity += gravity * timeStep;
            p.prevPosition = p.position;
            p.position += p.velocity * timeStep;
        }
    }
    
    void FindNeighbors()
    {
        float searchRadius = particleRadius * 2.0f;
        
        foreach (var p in particles)
        {
            p.neighbors.Clear();
            int particleIndex = particles.IndexOf(p);
            
            var nearbyIndices = spatialHash.Query(p.position, searchRadius);
            foreach (int otherIndex in nearbyIndices)
            {
                if (otherIndex != particleIndex)
                {
                    float dist = Vector3.Distance(p.position, particles[otherIndex].position);
                    if (dist < searchRadius)
                    {
                        p.neighbors.Add(otherIndex);
                    }
                }
            }
        }
    }
    
    void SolveConstraints()
    {
        // Calculate density and lambda for each particle
        foreach (var p in particles)
        {
            p.density = CalculateDensity(p);
            p.lambda = CalculateLambda(p);
        }
        
        // Calculate position corrections
        foreach (var p in particles)
        {
            Vector3 deltaP = Vector3.zero;
            
            // Density constraint
            foreach (int neighborIdx in p.neighbors)
            {
                Particle neighbor = particles[neighborIdx];
                Vector3 gradW = GradientSpiky(p.position - neighbor.position);
                deltaP += (p.lambda + neighbor.lambda) * gradW;
            }
            deltaP /= restDensity;
            
            // Cohesion force
            Vector3 cohesionForce = CalculateCohesion(p);
            deltaP += cohesionForce * cohesionStrength * timeStep * timeStep;
            
            // Surface tension
            Vector3 surfaceForce = CalculateSurfaceTension(p);
            deltaP += surfaceForce * surfaceTension * timeStep * timeStep;
            
            // Apply correction
            p.position += deltaP;
        }
    }
    
    float CalculateDensity(Particle p)
    {
        float density = 0.0f;
        float h = particleRadius * 2.0f;
        
        foreach (int neighborIdx in p.neighbors)
        {
            Vector3 diff = p.position - particles[neighborIdx].position;
            float dist = diff.magnitude;
            if (dist < h)
            {
                density += Poly6Kernel(dist, h);
            }
        }
        
        return density;
    }
    
    float CalculateLambda(Particle p)
    {
        float densityConstraint = (p.density / restDensity) - 1.0f;
        
        float sumGradSquared = 0.0f;
        foreach (int neighborIdx in p.neighbors)
        {
            Vector3 grad = GradientSpiky(p.position - particles[neighborIdx].position);
            sumGradSquared += grad.sqrMagnitude;
        }
        
        float epsilon = 1e-6f;
        return -densityConstraint / (sumGradSquared + epsilon);
    }
    
    Vector3 CalculateCohesion(Particle p)
    {
        Vector3 cohesion = Vector3.zero;
        float h = particleRadius * 2.0f;
        
        foreach (int neighborIdx in p.neighbors)
        {
            Vector3 diff = particles[neighborIdx].position - p.position;
            float dist = diff.magnitude;
            
            if (dist > 0.0001f && dist < h)
            {
                cohesion += diff.normalized * Poly6Kernel(dist, h);
            }
        }
        
        return cohesion;
    }
    
    Vector3 CalculateSurfaceTension(Particle p)
    {
        // Simplified surface tension towards local center
        if (p.neighbors.Count < 3) return Vector3.zero;
        
        Vector3 center = Vector3.zero;
        foreach (int neighborIdx in p.neighbors)
        {
            center += particles[neighborIdx].position;
        }
        center /= p.neighbors.Count;
        
        return (center - p.position) * 0.5f;
    }
    
    void UpdateVelocities()
    {
        foreach (var p in particles)
        {
            p.velocity = (p.position - p.prevPosition) / timeStep;
            p.velocity *= damping;
        }
    }
    
    void ApplyViscosity()
    {
        foreach (var p in particles)
        {
            if (p.neighbors.Count == 0) continue;
            
            Vector3 avgVelocity = Vector3.zero;
            foreach (int neighborIdx in p.neighbors)
            {
                avgVelocity += particles[neighborIdx].velocity;
            }
            avgVelocity /= p.neighbors.Count;
            
            p.velocity = Vector3.Lerp(p.velocity, avgVelocity, viscosity);
        }
    }
    
    void HandleCollisions()
    {
        foreach (var p in particles)
        {
            // Simple ground plane collision
            if (p.position.y < -5.0f)
            {
                p.position.y = -5.0f;
                p.velocity.y = Mathf.Max(0, p.velocity.y);
                p.velocity.x *= (1.0f - friction);
                p.velocity.z *= (1.0f - friction);
            }
            
            // Sphere collision detection with scene objects
            Collider[] hitColliders = Physics.OverlapSphere(p.position, particleRadius, collisionMask);
            foreach (var collider in hitColliders)
            {
                Vector3 closestPoint = collider.ClosestPoint(p.position);
                Vector3 normal = (p.position - closestPoint).normalized;
                float penetration = particleRadius - Vector3.Distance(p.position, closestPoint);
                
                if (penetration > 0)
                {
                    p.position += normal * penetration;
                    
                    // Reflect velocity
                    float velocityAlongNormal = Vector3.Dot(p.velocity, normal);
                    if (velocityAlongNormal < 0)
                    {
                        p.velocity -= normal * velocityAlongNormal;
                        p.velocity *= (1.0f - friction);
                    }
                }
            }
        }
    }
    
    // Kernel functions
    float Poly6Kernel(float r, float h)
    {
        if (r >= h) return 0;
        float scale = 315.0f / (64.0f * Mathf.PI * Mathf.Pow(h, 9));
        float x = h * h - r * r;
        return scale * x * x * x;
    }
    
    Vector3 GradientSpiky(Vector3 r)
    {
        float h = particleRadius * 2.0f;
        float rLen = r.magnitude;
        if (rLen >= h || rLen < 0.0001f) return Vector3.zero;
        
        float scale = -45.0f / (Mathf.PI * Mathf.Pow(h, 6));
        float x = h - rLen;
        return scale * x * x * r.normalized;
    }
    
    void OnDrawGizmos()
    {
        if (!drawDebugSpheres || particles == null) return;
        
        Gizmos.color = debugColor;
        foreach (var p in particles)
        {
            Gizmos.DrawWireSphere(p.position, particleRadius);
        }
    }
    
    // Public accessors for rendering
    public List<Vector3> GetParticlePositions()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var p in particles)
        {
            positions.Add(p.position);
        }
        return positions;
    }
    
    public int GetParticleCount()
    {
        return particles.Count;
    }
}

/// <summary>
/// Spatial hash grid for fast neighbor finding
/// </summary>
public class SpatialHash
{
    private Dictionary<Vector3Int, List<int>> grid;
    private float cellSize;
    
    public SpatialHash(float cellSize)
    {
        this.cellSize = cellSize;
        this.grid = new Dictionary<Vector3Int, List<int>>();
    }
    
    public void Clear()
    {
        grid.Clear();
    }
    
    public void Add(Vector3 position, int index)
    {
        Vector3Int cell = GetCell(position);
        if (!grid.ContainsKey(cell))
        {
            grid[cell] = new List<int>();
        }
        grid[cell].Add(index);
    }
    
    public List<int> Query(Vector3 position, float radius)
    {
        List<int> result = new List<int>();
        
        int cellRadius = Mathf.CeilToInt(radius / cellSize);
        Vector3Int centerCell = GetCell(position);
        
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    Vector3Int cell = centerCell + new Vector3Int(x, y, z);
                    if (grid.ContainsKey(cell))
                    {
                        result.AddRange(grid[cell]);
                    }
                }
            }
        }
        
        return result;
    }
    
    private Vector3Int GetCell(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }
}
