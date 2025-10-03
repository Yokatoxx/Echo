using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GPU-accelerated version of SlimeParticleManager using compute shaders
/// Can handle 5-10x more particles than CPU version
/// </summary>
public class SlimeParticleManagerGPU : MonoBehaviour
{
    [Header("Compute Shader")]
    public ComputeShader physicsCompute;
    
    [Header("Particle Settings")]
    [Range(100, 5000)]
    public int maxParticles = 2000;
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
    public float friction = 0.1f;
    public float damping = 0.99f;
    
    [Header("Initial Setup")]
    public Vector3 initialSpawnCenter = Vector3.zero;
    public float initialSpawnRadius = 0.5f;
    
    [Header("Debug")]
    public bool drawDebugSpheres = false;
    public Color debugColor = Color.green;
    
    // Compute shader kernels
    private int integrateKernel;
    private int buildHashKernel;
    private int findNeighborsKernel;
    private int calculateDensityKernel;
    private int solveConstraintsKernel;
    private int updateVelocitiesKernel;
    private int applyViscosityKernel;
    private int handleCollisionsKernel;
    
    // Compute buffers
    private ComputeBuffer particleBuffer;
    private ComputeBuffer spatialHashBuffer;
    private ComputeBuffer neighborsBuffer;
    
    // CPU-side data for debugging
    private ParticleData[] particleDataArray;
    private Vector3[] particlePositions;
    
    private bool initialized = false;
    private float timeStep;
    
    // Particle structure matching compute shader
    struct ParticleData
    {
        public Vector3 position;
        public Vector3 prevPosition;
        public Vector3 velocity;
        public float invMass;
        public float density;
        public float lambda;
        public int neighborCount;
        public int neighborStartIndex;
    }
    
    struct NeighborData
    {
        public int index;
    }
    
    void Start()
    {
        InitializeGPU();
    }
    
    void InitializeGPU()
    {
        if (physicsCompute == null)
        {
            Debug.LogError("Physics compute shader not assigned!");
            enabled = false;
            return;
        }
        
        // Find kernels
        integrateKernel = physicsCompute.FindKernel("Integrate");
        buildHashKernel = physicsCompute.FindKernel("BuildSpatialHash");
        findNeighborsKernel = physicsCompute.FindKernel("FindNeighbors");
        calculateDensityKernel = physicsCompute.FindKernel("CalculateDensity");
        solveConstraintsKernel = physicsCompute.FindKernel("SolveConstraints");
        updateVelocitiesKernel = physicsCompute.FindKernel("UpdateVelocities");
        applyViscosityKernel = physicsCompute.FindKernel("ApplyViscosity");
        handleCollisionsKernel = physicsCompute.FindKernel("HandleCollisions");
        
        // Initialize particles
        InitializeParticles();
        
        // Create compute buffers
        int particleStride = sizeof(float) * 3 + // position
                            sizeof(float) * 3 + // prevPosition
                            sizeof(float) * 3 + // velocity
                            sizeof(float) +     // invMass
                            sizeof(float) +     // density
                            sizeof(float) +     // lambda
                            sizeof(int) +       // neighborCount
                            sizeof(int);        // neighborStartIndex
        
        particleBuffer = new ComputeBuffer(maxParticles, particleStride);
        spatialHashBuffer = new ComputeBuffer(maxParticles, sizeof(int));
        neighborsBuffer = new ComputeBuffer(maxParticles * 50, sizeof(int)); // Max 50 neighbors per particle
        
        particleBuffer.SetData(particleDataArray);
        
        // Set buffers on all kernels
        SetComputeBuffers();
        
        initialized = true;
        Debug.Log($"GPU slime system initialized with {maxParticles} particles");
    }
    
    void InitializeParticles()
    {
        particleDataArray = new ParticleData[maxParticles];
        particlePositions = new Vector3[maxParticles];
        
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
                        particleDataArray[count] = new ParticleData
                        {
                            position = pos,
                            prevPosition = pos,
                            velocity = Vector3.zero,
                            invMass = 1.0f,
                            density = restDensity,
                            lambda = 0,
                            neighborCount = 0,
                            neighborStartIndex = 0
                        };
                        particlePositions[count] = pos;
                        count++;
                    }
                }
            }
        }
        
        // Fill remaining with duplicates if needed
        for (int i = count; i < maxParticles; i++)
        {
            particleDataArray[i] = particleDataArray[0];
            particlePositions[i] = particlePositions[0];
        }
    }
    
    void SetComputeBuffers()
    {
        int[] kernels = { integrateKernel, buildHashKernel, findNeighborsKernel, 
                         calculateDensityKernel, solveConstraintsKernel, updateVelocitiesKernel,
                         applyViscosityKernel, handleCollisionsKernel };
        
        foreach (int kernel in kernels)
        {
            physicsCompute.SetBuffer(kernel, "_Particles", particleBuffer);
            physicsCompute.SetBuffer(kernel, "_SpatialHash", spatialHashBuffer);
            physicsCompute.SetBuffer(kernel, "_Neighbors", neighborsBuffer);
        }
    }
    
    void FixedUpdate()
    {
        if (!initialized) return;
        
        timeStep = Time.fixedDeltaTime / substeps;
        
        for (int substep = 0; substep < substeps; substep++)
        {
            SimulationStepGPU();
        }
        
        // Read back positions for rendering
        if (drawDebugSpheres)
        {
            particleBuffer.GetData(particleDataArray);
            for (int i = 0; i < maxParticles; i++)
            {
                particlePositions[i] = particleDataArray[i].position;
            }
        }
    }
    
    void SimulationStepGPU()
    {
        int threadGroups = Mathf.CeilToInt(maxParticles / 64.0f);
        
        // Set parameters
        physicsCompute.SetInt("_ParticleCount", maxParticles);
        physicsCompute.SetFloat("_DeltaTime", timeStep);
        physicsCompute.SetVector("_Gravity", gravity);
        physicsCompute.SetFloat("_ParticleRadius", particleRadius);
        physicsCompute.SetFloat("_RestDensity", restDensity);
        physicsCompute.SetFloat("_CohesionStrength", cohesionStrength);
        physicsCompute.SetFloat("_SurfaceTension", surfaceTension);
        physicsCompute.SetFloat("_Viscosity", viscosity);
        physicsCompute.SetFloat("_Damping", damping);
        physicsCompute.SetFloat("_Friction", friction);
        physicsCompute.SetFloat("_GridCellSize", particleRadius * 2.0f);
        
        // 1. Integrate
        physicsCompute.Dispatch(integrateKernel, threadGroups, 1, 1);
        
        // 2. Build spatial hash
        physicsCompute.Dispatch(buildHashKernel, threadGroups, 1, 1);
        
        // 3. Find neighbors
        physicsCompute.Dispatch(findNeighborsKernel, threadGroups, 1, 1);
        
        // 4. Solve constraints
        for (int iter = 0; iter < solverIterations; iter++)
        {
            physicsCompute.Dispatch(calculateDensityKernel, threadGroups, 1, 1);
            physicsCompute.Dispatch(solveConstraintsKernel, threadGroups, 1, 1);
        }
        
        // 5. Update velocities
        physicsCompute.Dispatch(updateVelocitiesKernel, threadGroups, 1, 1);
        
        // 6. Apply viscosity
        physicsCompute.Dispatch(applyViscosityKernel, threadGroups, 1, 1);
        
        // 7. Handle collisions
        physicsCompute.Dispatch(handleCollisionsKernel, threadGroups, 1, 1);
    }
    
    void OnDrawGizmos()
    {
        if (!drawDebugSpheres || particlePositions == null || !initialized) return;
        
        Gizmos.color = debugColor;
        for (int i = 0; i < Mathf.Min(maxParticles, particlePositions.Length); i++)
        {
            Gizmos.DrawWireSphere(particlePositions[i], particleRadius);
        }
    }
    
    void OnDestroy()
    {
        if (particleBuffer != null) particleBuffer.Release();
        if (spatialHashBuffer != null) spatialHashBuffer.Release();
        if (neighborsBuffer != null) neighborsBuffer.Release();
    }
    
    // Public accessors for rendering
    public ComputeBuffer GetParticleBuffer()
    {
        return particleBuffer;
    }
    
    public List<Vector3> GetParticlePositions()
    {
        if (!initialized) return new List<Vector3>();
        
        particleBuffer.GetData(particleDataArray);
        List<Vector3> positions = new List<Vector3>();
        
        for (int i = 0; i < maxParticles; i++)
        {
            positions.Add(particleDataArray[i].position);
        }
        
        return positions;
    }
    
    public int GetParticleCount()
    {
        return maxParticles;
    }
}
