using UnityEngine;

public class AutoDestroyParticle : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool useParticleSystemDuration = true;

    void Start()
    {
        if (useParticleSystemDuration)
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Destroy after particle duration + a bit extra for fade out
                lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            }
        }

        Destroy(gameObject, lifetime);
    }
}