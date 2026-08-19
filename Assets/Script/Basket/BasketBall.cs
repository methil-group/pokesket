using System;
using System.Collections;
using UnityEngine;
using Object = System.Object;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class BasketBall : MonoBehaviour
{
    private PokemonPlayer currentHolder => BasketBallManager.Instance.BallHolder;

    public ParticleSystem particle;
    public TrailRenderer trailRenderer;
    public Light ballLight;
    public LayerMask rimLayer;

    [NonSerialized] public int points = 3;

    public Rigidbody rb => GetComponent<Rigidbody>();

    public void Start()
    {
        StopEmitTrail();
    }

    public void StopEmitTrail()
    {
        var emission = particle.emission;
        emission.rateOverTime = 0f;
        trailRenderer.emitting = false;
        ballLight.intensity = 0;
    }

    public void StartEmitTrail(PokemonType pokemonType)
    {
        var emission = particle.emission;
        emission.rateOverTime = 12f;
        particle.gameObject.GetComponent<ParticleSystemRenderer>().material.SetColor("_TintColor", pokemonType.noHdrTypeColor);
        trailRenderer.material.SetTexture("_TintTex", pokemonType.trailTexture);
        trailRenderer.emitting = true;
        ballLight.color = pokemonType.noHdrTypeColor;
        ballLight.intensity = 7.7f;
    }

    public void Update()
    {
        if (currentHolder != null)
        {
            this.transform.rotation = Quaternion.identity;
            Vector3 offset = currentHolder.Direction * 0.5f;
            this.GetComponent<Rigidbody>().angularVelocity = new Vector3(rb.angularVelocity.x, rb.angularVelocity.y, 0f);
            transform.position = currentHolder.transform.position + offset;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (currentHolder == null)
            {
                BasketBallManager.Instance.ResetHolder();
                StopEmitTrail();
            }
        }
    }

    public void ShootTowardsBasket(Vector3 target, bool isSuccessful, float force, PokemonPlayer shooter)
    {
        points = 3;

        if (shooter != null)
        {
            var zoneDetector = gameObject.GetComponent<Zone2PtsDetector>();
            if (zoneDetector != null && shooter.Team != null && zoneDetector.IsInOpponent2PtsZone(shooter.Team.teamName))
            {
                points = 2;
            }
        }

        rb.useGravity = true;
        rb.isKinematic = false;

        // Assure la bonne taille et masse du ballon
        transform.localScale = Vector3.one;
        rb.mass = 0.62f;

        Vector3 start = transform.position;

        if (force >= 0.95f)
        {
            StartEmitTrail(shooter.actualPokemon.pokemonType);
            GameManager.Instance.CameraManager.SetNewLookAtTransform(this.transform);
            LeanTween.delayedCall(2.5f, StopEmitTrail);
        }

        Debug.LogWarning("Is shoot successful: " + isSuccessful);

        if (!isSuccessful)
        {
            float missRadius = 1.0f * (1f - UnityEngine.Random.value);
            Vector2 offset2D = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.2f, missRadius);
            Vector3 offset = new Vector3(offset2D.x, 0, offset2D.y);
            target += offset;
        }

        // Correction cible : centre du panier avec élévation
        target += Vector3.up * 0.5f;

        Vector3 velocity = CalculateArcVelocity(start, target, force, guaranteedPerfect: isSuccessful);
        rb.linearVelocity = velocity;

        if (shooter.Team.teamName == TeamName.Red)
        {
            rb.angularVelocity = new Vector3(rb.angularVelocity.x, rb.angularVelocity.y, -2f);
        }
        else
        {
            rb.angularVelocity = new Vector3(rb.angularVelocity.x, rb.angularVelocity.y, 3f);
        }
    }

    private Vector3 CalculateArcVelocity(Vector3 start, Vector3 end, float force, bool guaranteedPerfect = false)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);

        Vector3 displacementXZ = new Vector3(end.x - start.x, 0, end.z - start.z);
        float distance = displacementXZ.magnitude;

        float arcFactor = 0.3f;
        float minArcExtra = 1.0f;
        float maxArcExtra = 2.5f;
        float arcExtra = Mathf.Clamp(distance * arcFactor, minArcExtra, maxArcExtra);

        float apexHeight = Mathf.Max(start.y, end.y) + arcExtra;

        float timeToApex = Mathf.Sqrt(2 * (apexHeight - start.y) / gravity);
        float timeFromApex = Mathf.Sqrt(2 * (apexHeight - end.y) / gravity);
        float totalTime = timeToApex + timeFromApex;

        if (!guaranteedPerfect)
            totalTime /= Mathf.Clamp(force, 0.1f, 2f);

        Vector3 velocityXZ = displacementXZ / totalTime;
        float velocityY = Mathf.Sqrt(2 * gravity * (apexHeight - start.y));

        return velocityXZ + Vector3.up * velocityY;
    }

    public void PassTo(Vector3 target)
    {
        rb.useGravity = true;
        rb.isKinematic = false;

        Vector3 start = transform.position;
        Vector3 velocity = CalculatePassVelocity(start, target);

        rb.linearVelocity = velocity;
    }

    private Vector3 CalculatePassVelocity(Vector3 start, Vector3 end)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);

        Vector3 displacementXZ = new Vector3(end.x - start.x, 0, end.z - start.z);
        float horizontalDistance = displacementXZ.magnitude;

        float arcFactor = 0.05f;
        float minArcHeight = 0.5f;
        float maxArcHeight = 1.2f;
        float arcExtra = Mathf.Clamp(horizontalDistance * arcFactor, minArcHeight, maxArcHeight);

        float apexHeight = Mathf.Max(start.y, end.y) + arcExtra;

        float timeToApex = Mathf.Sqrt(2 * (apexHeight - start.y) / gravity);
        float timeFromApex = Mathf.Sqrt(2 * (apexHeight - end.y) / gravity);
        float totalTime = timeToApex + timeFromApex;

        Vector3 velocityXZ = displacementXZ / totalTime;
        float velocityY = Mathf.Sqrt(2 * gravity * (apexHeight - start.y));

        return velocityXZ + Vector3.up * velocityY;
    }

    public void DunkInto(Vector3 target)
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        // Désactiver temporairement les collisions avec le rim
        StartCoroutine(TemporarilyIgnoreRimCollisions());

        Vector3 direction = (target - transform.position).normalized;
        float speed = 12f;

        rb.linearVelocity = direction * speed;

        GameManager.Instance.CameraManager.SetNewLookAtTransform(this.transform);
        points = 2;
    }

    private IEnumerator TemporarilyIgnoreRimCollisions()
    {
        // Obtenir tous les colliders avec la Layer "Rim"
        Collider[] rimColliders = FindObjectsByType<Collider>();
        Collider ballCollider = GetComponent<Collider>();

        foreach (var c in rimColliders)
        {
            if ((rimLayer.value & (1 << c.gameObject.layer)) != 0)
            {
                Physics.IgnoreCollision(ballCollider, c, true);
            }
        }

        // Laisser le ballon traverser pendant un court instant
        yield return new WaitForSeconds(0.3f);

        // Réactiver les collisions
        foreach (var c in rimColliders)
        {
            if ((rimLayer.value & (1 << c.gameObject.layer)) != 0)
            {
                Physics.IgnoreCollision(ballCollider, c, false);
            }
        }
    }
}
