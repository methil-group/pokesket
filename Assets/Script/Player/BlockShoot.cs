using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PokemonPlayer))]
public class BlockShoot : MonoBehaviour
{
    [SerializeField] private float timingToReblock = 2f;
    [SerializeField] private float maxDashDistance = 2.5f;
    [SerializeField] private float dashDuration = 0.2f;

    private PokemonPlayer _pokemonPlayer;

    void Awake()
    {
        _pokemonPlayer = GetComponent<PokemonPlayer>();
    }

    void Update()
    {
        if (GameManager.Instance.matchPlaying == false) return;
        
        if (Input.GetKeyDown(_pokemonPlayer.ControlledByPlayer1 ? RemoteInput.B1 : RemoteInput.B2))
        {
            if (_pokemonPlayer.canBlock && BasketBallManager.Instance.IsBallHolded())
            {
                TryBlock();
            }
        }
    }

    private void TryBlock()
    {
        PokemonPlayer target = BasketBallManager.Instance.BallHolder;
        var dashPower = _pokemonPlayer.actualPokemon.defence / 100 + 0.5f;
        
        if (target != null && target != _pokemonPlayer)
        {
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            Vector3 dashTarget = transform.position + directionToTarget * maxDashDistance;
            StartCoroutine(DashTowards(dashTarget, success: true, dashPower));
        }
        else
        {
            // Dash dans la direction actuelle du joueur (pénalité si raté)
            Vector3 fallbackDir = _pokemonPlayer.Direction.normalized;
            Vector3 dashTarget = transform.position + fallbackDir * maxDashDistance;
            StartCoroutine(DashTowards(dashTarget, success: false, dashPower));
        }
    }

    private IEnumerator DashTowards(Vector3 dashTarget, bool success, float dashLengthMultiplier)
    {
        _pokemonPlayer.isBlockingShoot = true;
        _pokemonPlayer.canBlock = false;

        float elapsed = 0f;
        Vector3 startPos = transform.position;

        Vector3 direction = (dashTarget - startPos).normalized;
        float baseDistance = Vector3.Distance(startPos, dashTarget);
        float finalDistance = baseDistance * dashLengthMultiplier;
        Vector3 finalTarget = startPos + direction * finalDistance;
        finalTarget.y = startPos.y;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;
            transform.position = Vector3.Lerp(startPos, finalTarget, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        _pokemonPlayer.isBlockingShoot = false;

        if (!success)
        {
            yield return new WaitForSeconds(timingToReblock);
        }

        _pokemonPlayer.canBlock = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pokemon"))
        {
            PokemonPlayer opponent = other.GetComponent<PokemonPlayer>();
            if (opponent != null && opponent.Team != _pokemonPlayer.Team && opponent.isShooting && _pokemonPlayer.isBlockingShoot)
            {
                Debug.Log("Ball blocked");
                BasketBallManager.Instance.SetBallHolder(_pokemonPlayer);
            }
        }
    }
}
