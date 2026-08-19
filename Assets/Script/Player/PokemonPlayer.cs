using System;
using System.Collections;
using UnityEngine;

public class PokemonPlayer : MonoBehaviour
{
    public bool IsControlled => Team.IsControlled(this);
    public bool HasBall => BasketBallManager.Instance.IsPlayerHoldingBall(this);
    public bool TeamHasBall => BasketBallManager.Instance.IsTeamHoldedBall(Team);

    [Header("Stats")]
    [NonSerialized] public float initialSpeed;
    public float speed = 5f;

    [NonSerialized]
    public Pokemon actualPokemon;
    public float shootPrecision
    {
        get => actualPokemon.shootPrecision;
    }

    [Header("References")]
    public BasketTeam Team;
    public bool ControlledByPlayer1
    {
        get { return Team.teamName == TeamName.Blue; }
    }
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private SpriteRenderer teamIndicator;
    [SerializeField] private SpriteRenderer pokemonSpriteRenderer;
    [Header("Scripts")]
    [SerializeField] public ShootPlayer shootPlayer;
    [SerializeField] public PassPlayer passPlayer;
    [SerializeField] public DunkPlayer dunkPlayer;
    [SerializeField] public BlockShoot blockShoot;
    [SerializeField] public BlockPass blockPass;

    [NonSerialized] Vector3 lastMoveDirection = Vector3.up;
    public Vector3 Direction => lastMoveDirection;
    // BOOLEANS
    [NonSerialized] public bool canPass = true;
    [NonSerialized] public bool canShoot = true;
    [NonSerialized] public bool canDunk = true;
    [NonSerialized] public bool isShooting = false;
    [NonSerialized] public bool isPassing = false;
    [NonSerialized] public bool canBlock = true;
    [NonSerialized] public bool isBlockingShoot = false;
    [NonSerialized] public bool isBlockingPass = false;
    private bool _canHold = true;
    
    [NonSerialized] public int pointScored;

    private IPokemonPlayerState currentState;

    [SerializeField]
    public PokemonPlayerAnimator pokemonPlayerAnimator;

    [SerializeField]
    public PokemonRole role;

    public void Setup(Pokemon pokemon)
    {
        actualPokemon = pokemon;
        pokemonSpriteRenderer.sprite = pokemon.pokemonSprite;
        initialSpeed = pokemon.speed;
        speed = pokemon.speed;
        currentState = new AIDefenseState(this);
        indicator.color = Team.teamName == TeamName.Red ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 1f);
        teamIndicator.color = Team.teamName == TeamName.Red ? Color.red : Color.blue;
    }

    void Update()
    {
        if (GameManager.Instance.matchPlaying == false) return;

        currentState?.Update();
        currentState?.HandleMovement();
        
        // Limit position in the terrain, without using collider
        LimitPosition();

        indicator.gameObject.SetActive(IsControlled);
    }

    public void ApplyMovement(Vector3 move)
    {
        if (move.sqrMagnitude > 0.01f)
            lastMoveDirection = move.normalized;

        transform.Translate(move * speed * Time.deltaTime);

        try
        {
            pokemonPlayerAnimator.HandleAnimation(move, actualPokemon);
        }
        catch (Exception e)
        {
            string pokemonName = actualPokemon != null ? actualPokemon.pokemonName : "<unknown>";
            Debug.LogWarning("Error animating pokemon : " + pokemonName + " : " + e);
        }
    }

    public void LimitPosition()
    {
        if(this.transform.position.x < -11f) this.transform.position = new Vector3(-10.99f, this.transform.position.y, this.transform.position.z);
        if(this.transform.position.x > 11f) this.transform.position = new Vector3(10.99f, this.transform.position.y, this.transform.position.z);
        
        if(this.transform.position.z < -10.5f) this.transform.position = 
            new Vector3(this.transform.position.x, this.transform.position.y, -10.49f);
        if(this.transform.position.z > 9.5f) this.transform.position = 
            new Vector3(this.transform.position.x, this.transform.position.y, 9.49f);
    }

    public void UpdateState(IPokemonPlayerState newState)
    {
        currentState = newState;
    }

    void OnTriggerEnter(Collider other)
    {
        bool isHolded = BasketBallManager.Instance.IsBallHolding();
        if (!HasBall && !isHolded && _canHold)
        {
            BasketBall ball = other.GetComponent<BasketBall>();
            if (ball != null)
            {
                StartCoroutine(DisableAction());
                BasketBallManager.Instance.SetBallHolder(this);
                if (GameManager.Instance == null || GameManager.Instance.IsTeamHumanControlled(Team))
                {
                    Team.SetControlledPlayer(this);
                }
            }
        }
    }

    public void ShootBall()
    {
        LoseBall();
        StartCoroutine(ShootBallCoroutine());
    }

    IEnumerator ShootBallCoroutine()
    {
        isShooting = true;
        yield return new WaitForSeconds(0.1f);
        isShooting = false;
    }

    public void PassBall()
    {
        LoseBall();
        StartCoroutine(PassBallCoroutine());
    }

    IEnumerator PassBallCoroutine()
    {
        isPassing = true;
        yield return new WaitForSeconds(0.1f);
        isPassing = false;
    }

    public void DunkBall()
    {
        _canHold = false;
        StartCoroutine(DunkBallCoroutine());
    }

    IEnumerator DunkBallCoroutine()
    {
        yield return new WaitForSeconds(1.3f);
        _canHold = true;
    }

    public void LoseBall()
    {
        StartCoroutine(LoseBallCoroutine());
    }

    IEnumerator LoseBallCoroutine()
    {
        _canHold = false;
        yield return new WaitForSeconds(0.5f);
        _canHold = true;
    }

    IEnumerator DisableAction()
    {
        canPass = false;
        canShoot = false;
        canDunk = false;
        yield return new WaitForSeconds(0.1f);
        canPass = true;
        canShoot = true;
        canDunk = true;
    }
    
    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (currentState == null) return;

        var style = new GUIStyle
        {
            normal = { textColor = Color.white },
            alignment = TextAnchor.UpperCenter,
            fontSize = 12
        };

        Vector3 labelPosition = transform.position + Vector3.up * 0.2f;
        UnityEditor.Handles.Label(labelPosition, currentState.ToString(), style);
#endif
    }
}

[Serializable]
public enum PokemonRole
{
    Top,
    Front,
    Bottom
}
