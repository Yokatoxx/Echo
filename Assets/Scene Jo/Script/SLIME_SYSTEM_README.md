# Slime Physics System - Documentation

## Overview
This is a Position Based Fluids (PBF) slime simulation system with cohesion, surface tension, and metaball rendering capabilities.

## Components

### 1. SlimeParticleManager
The core physics simulation component that manages all particles and their interactions.

**Key Features:**
- Position Based Fluids (PBF) simulation
- Spatial hash grid for efficient neighbor finding (O(n) complexity)
- Density constraints using Poly6 kernel
- Cohesion forces to keep particles together
- Surface tension for realistic blob behavior
- Viscosity smoothing for fluid-like motion
- Collision detection with scene objects
- Configurable substeps for stability

**Parameters:**
- `maxParticles` (100-2000): Maximum number of particles in the simulation
- `particleRadius` (0.03-0.15): Radius of each particle
- `restDensity`: Target density (normalized to 1.0)
- `solverIterations` (1-10): More iterations = more stable but slower
- `gravity`: Gravity vector
- `cohesionStrength` (0.01-0.5): How strongly particles stick together
- `surfaceTension` (0.0-0.3): Surface tension strength
- `viscosity` (0.1-0.6): Fluid viscosity (higher = more viscous)
- `substeps` (1-4): Physics substeps per frame for stability
- `collisionMask`: Which layers the slime collides with
- `friction`: Collision friction
- `damping`: Velocity damping

**How It Works:**
1. Integration: Applies gravity and predicts new positions
2. Neighbor Finding: Uses spatial hash to find nearby particles
3. Constraint Solving: Solves density, cohesion, and surface tension constraints iteratively
4. Velocity Update: Updates velocities based on position changes
5. Viscosity: Smooths velocities with neighbors
6. Collisions: Handles collisions with scene objects

### 2. SlimeRenderer
Renders the slime particles using different methods.

**Render Modes:**
- **Instanced Spheres**: Fast debug rendering using instanced sphere meshes
- **Metaballs**: Advanced rendering with transparency and Fresnel effects

**Parameters:**
- `renderMode`: Choose between InstancedSpheres or Metaballs
- `sphereMesh`: Mesh to use for sphere rendering (auto-generated if null)
- `sphereMaterial`: Material for spheres
- `sphereScale`: Scale multiplier for rendered spheres
- `metaballMaterial`: Material for metaball rendering
- `metaballInfluenceRadius`: Influence radius for metaballs
- `slimeColor`: Color of the slime

### 3. SlimeAbsorption
Handles object absorption gameplay mechanics.

**Features:**
- Detects nearby absorbable objects
- Pulls objects towards slime center of mass
- Dissolves absorbed objects
- Optional particle effects and sound

**Parameters:**
- `absorbableLayers`: Which layers can be absorbed
- `absorptionRadius`: Detection radius
- `absorptionSpeed`: How fast objects are pulled in
- `spawnNewParticles`: Whether to spawn new particles from absorbed objects
- `particlesPerObject`: How many particles to spawn per object
- `absorptionEffect`: Particle effect prefab
- `absorptionSound`: Audio clip to play

### 4. SlimeMetaball.shader
Custom shader for rendering slime with realistic effects.

**Features:**
- Fresnel effect for edge glow
- Transparency with proper alpha blending
- Metallic and smoothness controls
- Emission based on viewing angle

## Setup Instructions

### Basic Setup
1. Create an empty GameObject in your scene
2. Add the `SlimeParticleManager` component
3. Add the `SlimeRenderer` component
4. Configure the parameters in the Inspector
5. Press Play to see the slime simulation

### Recommended Settings for Different Behaviors

**Thick, Viscous Slime:**
- viscosity: 0.5
- cohesionStrength: 0.05
- surfaceTension: 0.15
- solverIterations: 6

**Fluid, Watery Slime:**
- viscosity: 0.2
- cohesionStrength: 0.02
- surfaceTension: 0.05
- solverIterations: 4

**Bouncy, Elastic Slime:**
- viscosity: 0.3
- cohesionStrength: 0.04
- surfaceTension: 0.2
- damping: 0.95
- solverIterations: 8

### Adding Absorption
1. Add the `SlimeAbsorption` component to the slime GameObject
2. Set the `absorbableLayers` to the layers you want to absorb
3. Configure absorption parameters
4. Objects tagged with those layers will be absorbed when close enough

### Performance Optimization

**For Better Performance:**
- Reduce `maxParticles` (try 400-600)
- Reduce `solverIterations` (try 3-4)
- Use `InstancedSpheres` render mode instead of Metaballs
- Increase `particleRadius` slightly (fewer particles needed)
- Reduce `substeps` to 1-2

**For Better Quality:**
- Increase `maxParticles` (try 1000-1500)
- Increase `solverIterations` (try 6-8)
- Use `Metaballs` render mode
- Decrease `particleRadius` (more detailed)
- Increase `substeps` to 3-4

## Technical Details

### Physics Implementation
The system uses Position Based Dynamics (PBD) with Position Based Fluids (PBF) constraints:

1. **Density Constraint**: Keeps local density close to rest density
   - Uses Poly6 kernel for density calculation
   - Spiky kernel for gradients
   - Lagrange multipliers for constraint solving

2. **Cohesion**: Attracts nearby particles to prevent dispersion
   - Based on weighted average of neighbor positions
   - Helps maintain blob-like behavior

3. **Surface Tension**: Pulls surface particles towards local center
   - Calculated based on local curvature
   - Creates smooth surface behavior

4. **Viscosity**: Smooths velocity differences between neighbors
   - Uses Laplacian smoothing
   - Creates fluid-like motion

### Spatial Hash Grid
- O(n) complexity for neighbor finding
- Grid cell size = 2 × particle radius
- Checks 27 neighboring cells (3×3×3 grid)

### Collision Detection
- Sphere-based collision detection
- Uses Physics.OverlapSphere for scene objects
- Applies friction and damping
- Position correction for penetration resolution

## Passing Through Gaps

The slime naturally passes through gaps based on particle size:
- If gap > particle diameter: particles flow through
- Cohesion pulls trailing particles through
- Small particles may get left behind and reform
- Increase cohesion to make slime less likely to split

## Known Limitations

1. **Performance**: CPU-based simulation, scales O(n²) without spatial hash
2. **Particle Count**: Limited to ~2000 particles on standard hardware
3. **Rendering**: Simple instanced spheres, not true metaballs surface extraction
4. **Absorption**: Object absorption doesn't spawn new particles yet (commented out)

## Future Enhancements

- GPU compute shader for physics (10× speedup)
- Marching cubes for true metaball surface
- Dynamic particle spawning/despawning
- Temperature-based viscosity
- Cluster detection for split blobs
- Better rendering with screen-space effects

## Troubleshooting

**Slime explodes or jitters:**
- Increase `solverIterations`
- Decrease time step (increase substeps)
- Increase `surfaceTension`
- Check for NaN values in positions

**Slime disperses too easily:**
- Increase `cohesionStrength`
- Increase `surfaceTension`
- Decrease `viscosity` slightly

**Slime falls through floor:**
- Check collision layer mask
- Ensure colliders are on correct layer
- Increase `solverIterations`
- Add more substeps

**Performance issues:**
- Reduce `maxParticles`
- Reduce `solverIterations`
- Use InstancedSpheres render mode
- Disable debug sphere rendering
- Increase `particleRadius` (need fewer particles)

## References

Based on the research paper:
"Position Based Fluids" by Macklin and Müller (2013)

Implementation includes:
- PBF density constraints
- Cohesion forces
- Surface tension
- XSPH viscosity
