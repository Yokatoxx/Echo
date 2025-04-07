using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class SonarParticleGenerator : MonoBehaviour
{
    [Header("Particle Settings")]
    public ParticleSystem particlePrefab;
    public int maxParticles = 50;
    public float particleLifetime = 1.5f;
    public float minParticleSize = 0.1f;
    public float maxParticleSize = 0.3f;
    
    [Header("Emission Settings")]
    public int particlesPerCollision = 5;
    public float emissionRadius = 0.3f;
    public Vector2 velocityRange = new Vector2(0.2f, 1.0f);
    
    [Header("Color Settings")]
    public Gradient particleColors;
    public bool useWaveColorForParticles = true;
    
    // Référence privée au renderer du sonar
    private Renderer sonarRenderer;
    private List<ParticleSystem> particlePool = new List<ParticleSystem>();
    private int currentParticleIndex = 0;
    
    void Start()
    {
        sonarRenderer = GetComponent<Renderer>();
        
        // Créer un pool de systèmes de particules
        for (int i = 0; i < maxParticles; i++)
        {
            ParticleSystem newSystem = Instantiate(particlePrefab, transform.position, Quaternion.identity);
            newSystem.gameObject.SetActive(false);
            particlePool.Add(newSystem);
        }
        
        // Configurer le collider en trigger
        GetComponent<Collider>().isTrigger = true;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Ignorer les collisions avec les triggers et le joueur si nécessaire
        if (other.isTrigger || other.CompareTag("Player"))
            return;
            
        // Calculer le point d'intersection
        Vector3 intersectionPoint = CalculateIntersectionPoint(other);
        
        // Émettre des particules à ce point
        EmitParticlesAt(intersectionPoint, other.transform.up);
    }
    
    Vector3 CalculateIntersectionPoint(Collider other)
    {
        // Méthode simple pour estimer le point d'intersection
        // Cette méthode peut être améliorée avec un Raycast plus précis si nécessaire
        Vector3 direction = (other.transform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, other.transform.position);
        
        // Utilisez la taille actuelle du sonar comme distance approximative
        float scale = transform.localScale.x / 2.0f;
        
        return transform.position + direction * Mathf.Min(distance, scale);
    }
    
    void EmitParticlesAt(Vector3 position, Vector3 normal)
    {
        // Obtenir un système de particules du pool
        ParticleSystem ps = particlePool[currentParticleIndex];
        currentParticleIndex = (currentParticleIndex + 1) % maxParticles;
        
        // Configurer la position et activer
        ps.transform.position = position;
        ps.gameObject.SetActive(true);
        
        // Configurer les paramètres des particules
        var main = ps.main;
        main.startLifetime = particleLifetime;
        main.startSize = Random.Range(minParticleSize, maxParticleSize);
        
        // Utiliser la couleur du sonar si demandé
        if (useWaveColorForParticles && sonarRenderer != null)
        {
            Color waveColor = sonarRenderer.material.GetColor("_WaveColor");
            main.startColor = waveColor;
        }
        else
        {
            main.startColor = particleColors;
        }
        
        // Configurer la forme d'émission
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = emissionRadius;
        
        // Configurer la vélocité
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        
        // Émettre les particules
        ps.Emit(particlesPerCollision);
        
        // Arrêter l'émission après le burst initial
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}