using System.Collections;
using UnityEngine;

public class AIAttackState : IPokemonPlayerState
{
    private PokemonPlayer _pokemonPlayer;
    private float speed { get { return _pokemonPlayer.speed; }}

    public AIAttackState(PokemonPlayer pokemonPlayer)
    {
        _pokemonPlayer = pokemonPlayer;
        _pokemonPlayer.speed = speed;
        _pokemonPlayer.shootPlayer.enabled = false;
        _pokemonPlayer.passPlayer.enabled = false;
        _pokemonPlayer.dunkPlayer.enabled = false;
        _pokemonPlayer.blockShoot.enabled = false;
        _pokemonPlayer.blockPass.enabled = false;
    }

    readonly Vector3 UnsetVector3 = new Vector3(-1000, -1000, -1000);
    private Vector3 nextPosition = new Vector3(-1000, -1000, -1000);
    private const float ShootDistance = 11f;
    private const float ActionCooldown = 1.5f;
    private float nextActionTime;

    public void Update()
    {
        if (_pokemonPlayer.IsControlled)
        {
            _pokemonPlayer.UpdateState(new PlayerAttackState(_pokemonPlayer));
            return;
        }

        if (!_pokemonPlayer.TeamHasBall)
        {
            _pokemonPlayer.UpdateState(new AIDefenseState(_pokemonPlayer));
            return;
        }

        if (TryShoot()) return;
        AIMovement();
    }

    private bool TryShoot()
    {
        if (!_pokemonPlayer.HasBall || Time.time < nextActionTime) return false;

        Transform opponentRim = _pokemonPlayer.Team.GetOpponentRim();
        if (opponentRim == null) return false;

        Vector3 flatDistance = opponentRim.position - _pokemonPlayer.transform.position;
        flatDistance.y = 0f;
        if (flatDistance.magnitude > ShootDistance) return false;

        _pokemonPlayer.ShootBall();
        BasketBallManager.Instance.ShootTo(opponentRim, true, 1f, _pokemonPlayer);
        nextActionTime = Time.time + ActionCooldown;
        nextPosition = UnsetVector3;
        return true;
    }

    public void AIMovement()
    {
        if (nextPosition == UnsetVector3)
        {
            nextPosition = SetupNextPosition();
        }   
    }


    private GameObject GetZone() {
        BasketTeam enemyTeam = GameManager.Instance.GetTeam(TeamName.Blue);
        if (_pokemonPlayer.Team.teamName == TeamName.Blue)
        {
            enemyTeam = GameManager.Instance.GetTeam(TeamName.Red);
        }
        
        switch (_pokemonPlayer.role)
        {
            case PokemonRole.Top:
                return enemyTeam.TopZone;
            case PokemonRole.Front:
                return enemyTeam.FrontZone;
            case PokemonRole.Bottom:
                return enemyTeam.BottomZone;
        }
        return enemyTeam.BottomZone;
    }

    public Vector3 SetupNextPosition()
    {
        GameObject defenseZone = GetZone();
        if (defenseZone == null)
        {
            Debug.LogError("AI attack zone is not configured.");
            return _pokemonPlayer.transform.position;
        }

        Renderer renderer = defenseZone.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("Le GameObject n'a pas de Renderer.");
            return _pokemonPlayer.transform.position;
        }

        Bounds bounds = renderer.bounds;

        Vector3 randomPosition = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            _pokemonPlayer.gameObject.transform.position.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );

        return randomPosition;
    }

    public void HandleMovement()
    {
        if (nextPosition == UnsetVector3)
        {
            nextPosition = SetupNextPosition();
        }

        Vector3 direction = nextPosition - _pokemonPlayer.transform.position;
        direction.y = 0f; // Si tu veux ignorer le mouvement vertical (souvent le cas dans un jeu 2D/3D avec déplacement au sol)

        if (direction.magnitude < 0.1f) // Arrivé à destination
        {
            nextPosition = UnsetVector3; // On force le recalcul d'une nouvelle position
            return;
        }

        Vector3 move = direction.normalized;
        _pokemonPlayer.ApplyMovement(move);
    }
}
