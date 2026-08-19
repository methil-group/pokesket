using UnityEngine;

[RequireComponent(typeof(PokemonPlayer))]
public class PassPlayer : MonoBehaviour
{
    private PokemonPlayer _pokemonPlayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _pokemonPlayer = GetComponent<PokemonPlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.matchPlaying == false) return;
        
        if (Input.GetKeyDown(_pokemonPlayer.ControlledByPlayer1 ? RemoteInput.A1 : RemoteInput.A2))
        {
            PassBall();
        }
    }

    public void PassBall()
    {
        if (!_pokemonPlayer.canPass || !_pokemonPlayer.IsControlled || !_pokemonPlayer.HasBall) return;
        
        PokemonPlayer target = GetTargetAllie();
        if (target == null)
        {
            Debug.LogWarning("Aucun coéquipier autre que soi");
            return;
        }

        _pokemonPlayer.Team.SetControlledPlayer(target);
        _pokemonPlayer.PassBall();
        BasketBallManager.Instance.PassTo(target.transform);
    }

    private PokemonPlayer GetTargetAllie()
    {
        float h = _pokemonPlayer.ControlledByPlayer1 ? Input.GetAxis("HorizontalJoystick1") : Input.GetAxis("HorizontalJoystick2");
        float v = _pokemonPlayer.ControlledByPlayer1 ? Input.GetAxis("VerticalJoystick1") : Input.GetAxis("VerticalJoystick2");
        Vector2 inputDir = new Vector2(h, v);
        Vector3 myPosition = _pokemonPlayer.transform.position;

        Vector3? direction = null;
        if (inputDir.sqrMagnitude >= 0.1f)
            direction = new Vector3(inputDir.x, 0, inputDir.y).normalized;

        PokemonPlayer bestTargetInDirection = null;
        float bestScoreInDirection = float.MaxValue;
        PokemonPlayer closestAlly = null;
        float closestDistance = float.MaxValue;
        float maxAngle = 60f;
        int teammateCount = 0;

        foreach (var ally in _pokemonPlayer.Team.pokeTeam)
        {
            if (ally == _pokemonPlayer) continue;
            teammateCount++;

            Vector3 toAlly = ally.transform.position - myPosition;
            float distance = toAlly.sqrMagnitude;
            if (distance < closestDistance) { closestDistance = distance; closestAlly = ally; }

            if (direction.HasValue)
            {
                float angle = Vector3.Angle(direction.Value, toAlly);
                if (angle > maxAngle) continue;
                if (distance < bestScoreInDirection)
                {
                    bestScoreInDirection = distance;
                    bestTargetInDirection = ally;
                }
            }
        }

        return teammateCount == 0 ? null : bestTargetInDirection ?? closestAlly;
    }
}
