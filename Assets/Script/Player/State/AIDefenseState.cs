using UnityEngine;

public class AIDefenseState : IPokemonPlayerState
{
    private PokemonPlayer _pokemonPlayer;
    private float speed { get { return _pokemonPlayer.speed; }}

    public AIDefenseState(PokemonPlayer pokemonPlayer)
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
    private Vector3 movement;

    public void Update()
    {
        if (_pokemonPlayer.IsControlled)
        {
            _pokemonPlayer.UpdateState(new PlayerDefenseState(_pokemonPlayer));
            return;
        }

        if (_pokemonPlayer.TeamHasBall)
        {
            _pokemonPlayer.UpdateState(new AIAttackState(_pokemonPlayer));
            return;
        }
        AIMovement();
    }

    public void AIMovement()
    {
        if (nextPosition == UnsetVector3)
        {
            nextPosition = SetupNextPosition();
        }
    }

    public Vector3 SetupNextPosition()
    {
        GameObject defenseZone = GetZone();
        if (defenseZone == null)
        {
            Debug.LogError("AI defense zone is not configured.");
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

    private GameObject GetZone() {
        switch (_pokemonPlayer.role)
        {
            case PokemonRole.Top:
                return _pokemonPlayer.Team.TopZone;
            case PokemonRole.Front:
                return _pokemonPlayer.Team.FrontZone;
            case PokemonRole.Bottom:
                return _pokemonPlayer.Team.BottomZone;
        }
        return _pokemonPlayer.Team.BottomZone;
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
