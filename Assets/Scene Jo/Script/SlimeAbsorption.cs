using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles object absorption by slime - objects that get close enough are absorbed
/// Works with both SlimeParticleManager (CPU) and SlimeParticleManagerGPU
/// </summary>
public class SlimeAbsorption : MonoBehaviour
{
    [Header("Absorption Settings")]
    public LayerMask absorbableLayers;
    [Range(0.1f, 2.0f)]
    public float absorptionRadius = 0.5f;
    public float absorptionSpeed = 1.0f;
    public bool spawnNewParticles = true;
    [Range(1, 20)]
    public int particlesPerObject = 5;
    
    [Header("Visual Effects")]
    public ParticleSystem absorptionEffect;
    public AudioClip absorptionSound;
    
    private SlimeParticleManager particleManager;
    private SlimeParticleManagerGPU particleManagerGPU;
    private AudioSource audioSource;
    private HashSet<GameObject> absorbingObjects = new HashSet<GameObject>();
    private bool useGPU = false;
    
    void Start()
    {
        particleManager = GetComponent<SlimeParticleManager>();
        particleManagerGPU = GetComponent<SlimeParticleManagerGPU>();
        
        if (particleManager == null && particleManagerGPU == null)
        {
            Debug.LogError("SlimeAbsorption requires either SlimeParticleManager or SlimeParticleManagerGPU component!");
            enabled = false;
            return;
        }
        
        useGPU = (particleManagerGPU != null);
        
        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f;
        audioSource.maxDistance = 10.0f;
    }
    
    void Update()
    {
        CheckForAbsorbableObjects();
        ProcessAbsorbingObjects();
    }
    
    void CheckForAbsorbableObjects()
    {
        var positions = useGPU ? particleManagerGPU.GetParticlePositions() : particleManager.GetParticlePositions();
        
        // Check from center of mass
        if (positions.Count == 0) return;
        
        Vector3 centerOfMass = Vector3.zero;
        foreach (var pos in positions)
        {
            centerOfMass += pos;
        }
        centerOfMass /= positions.Count;
        
        // Find nearby objects
        Collider[] colliders = Physics.OverlapSphere(centerOfMass, absorptionRadius * 2.0f, absorbableLayers);
        
        foreach (var collider in colliders)
        {
            // Check if object is close to any particle
            bool isNearby = false;
            foreach (var pos in positions)
            {
                if (Vector3.Distance(pos, collider.transform.position) < absorptionRadius)
                {
                    isNearby = true;
                    break;
                }
            }
            
            if (isNearby && !absorbingObjects.Contains(collider.gameObject))
            {
                StartAbsorption(collider.gameObject);
            }
        }
    }
    
    void StartAbsorption(GameObject obj)
    {
        // Check if object can be absorbed
        var rigidbody = obj.GetComponent<Rigidbody>();
        if (rigidbody == null) return;
        
        absorbingObjects.Add(obj);
        
        // Play absorption effect
        if (absorptionEffect != null)
        {
            ParticleSystem effect = Instantiate(absorptionEffect, obj.transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2.0f);
        }
        
        if (absorptionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(absorptionSound);
        }
        
        Debug.Log($"Starting absorption of {obj.name}");
    }
    
    void ProcessAbsorbingObjects()
    {
        List<GameObject> toRemove = new List<GameObject>();
        
        foreach (var obj in absorbingObjects)
        {
            if (obj == null)
            {
                toRemove.Add(obj);
                continue;
            }
            
            // Pull object towards slime center
            var positions = useGPU ? particleManagerGPU.GetParticlePositions() : particleManager.GetParticlePositions();
            if (positions.Count == 0) continue;
            
            Vector3 centerOfMass = Vector3.zero;
            foreach (var pos in positions)
            {
                centerOfMass += pos;
            }
            centerOfMass /= positions.Count;
            
            Vector3 direction = (centerOfMass - obj.transform.position).normalized;
            float distance = Vector3.Distance(obj.transform.position, centerOfMass);
            
            // Move object towards center
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = direction * absorptionSpeed;
            }
            else
            {
                obj.transform.position += direction * absorptionSpeed * Time.deltaTime;
            }
            
            // Check if close enough to fully absorb
            float particleRadius = useGPU ? particleManagerGPU.particleRadius : particleManager.particleRadius;
            if (distance < particleRadius * 2.0f)
            {
                AbsorbObject(obj);
                toRemove.Add(obj);
            }
        }
        
        // Remove absorbed objects from tracking
        foreach (var obj in toRemove)
        {
            absorbingObjects.Remove(obj);
        }
    }
    
    void AbsorbObject(GameObject obj)
    {
        Debug.Log($"Absorbed {obj.name}");
        
        // Optional: spawn new particles (would need to modify particle managers to support this)
        int maxParticles = useGPU ? particleManagerGPU.maxParticles : particleManager.maxParticles;
        int currentCount = useGPU ? particleManagerGPU.GetParticleCount() : particleManager.GetParticleCount();
        if (spawnNewParticles && currentCount < maxParticles)
        {
            // This would require adding a method to SlimeParticleManager to add particles dynamically
            // For now, just destroy the object
        }
        
        // Destroy the absorbed object
        Destroy(obj);
    }
    
    void OnDrawGizmosSelected()
    {
        if (particleManager == null && particleManagerGPU == null) return;
        
        var positions = useGPU ? particleManagerGPU.GetParticlePositions() : particleManager.GetParticlePositions();
        if (positions.Count == 0) return;
        
        // Draw absorption radius around slime center
        Vector3 centerOfMass = Vector3.zero;
        foreach (var pos in positions)
        {
            centerOfMass += pos;
        }
        centerOfMass /= positions.Count;
        
        Gizmos.color = new Color(1, 0, 1, 0.3f);
        Gizmos.DrawWireSphere(centerOfMass, absorptionRadius * 2.0f);
    }
}
