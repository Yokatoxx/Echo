using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(Collider))]
public class ThrowableSoundObject : MonoBehaviour
{
    [Header("Configuration FMOD")]
    [SerializeField] private EventReference collisionEvent;

    [Header("Paramètres")]

    [SerializeField] private float minVelocityForSound = 0.8f;

    [SerializeField] private float soundCooldown = 0.2f;

    [SerializeField] private string surfaceTypeParameter = "SurfaceType";

    [SerializeField] private string impactForceParameter = "ImpactForce";

    public enum SurfaceType
    {
        Wood = 0,
        Tile = 1,
        Carpet = 2
    }

    private float lastSoundTime = -1f;
    private Rigidbody rb;
    private bool isHeld = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.LogWarning($"Rigidbody ajouté automatiquement à {gameObject.name}");
        }
    }

    public void OnGrab() => isHeld = true;
    public void OnRelease() => isHeld = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (isHeld)
            return;
        if (Time.time < lastSoundTime + soundCooldown)
            return;
        float impactVelocity = collision.relativeVelocity.magnitude;
        if (impactVelocity < minVelocityForSound)
            return;

        SurfaceType surfaceType = GetSurfaceTypeFromTag(collision.gameObject.tag);
        float normalizedForce = Mathf.Clamp01(impactVelocity / 10f);
        PlayCollisionSound(collision.contacts[0].point, normalizedForce, surfaceType);

        lastSoundTime = Time.time;
    }

    private void PlayCollisionSound(Vector3 position, float normalizedForce, SurfaceType surfaceType)
    {
        if (collisionEvent.IsNull)
            return;

        FMOD.Studio.EventInstance instance = RuntimeManager.CreateInstance(collisionEvent);

        if (instance.isValid())
        {
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            float surfaceValue = GetSurfaceTypeValue(surfaceType);
            instance.setParameterByName(surfaceTypeParameter, surfaceValue);
            instance.setParameterByName(impactForceParameter, normalizedForce);
            instance.start();
            instance.release();
        }
    }

    private float GetSurfaceTypeValue(SurfaceType surfaceType)
    {
        switch (surfaceType)
        {
            case SurfaceType.Wood: return 0.5f;
            case SurfaceType.Tile: return 1.5f;
            case SurfaceType.Carpet: return 2.5f;
            default: return 0.5f; // Par défaut
        }
    }

    private SurfaceType GetSurfaceTypeFromTag(string tag)
    {
        switch (tag)
        {
            case "TileSurface": return SurfaceType.Tile;
            case "CarpetSurface": return SurfaceType.Carpet;
            case "WoodSurface":
            default: return SurfaceType.Wood; // Par défaut
        }
    }
}

