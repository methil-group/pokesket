using System;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class BasketBallManager : MonoBehaviour
{
    public static BasketBallManager Instance;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform ballSpawnPoint;
    [SerializeField] private float timeBeforeReset = 3f;
    public BasketBall basketBall;
    // If can be hold by team is null, everyone can get the ball,
    // if it's to a team, the ball is deserved to a team
    [NonSerialized] public TeamName? canBeHoldByTeam = null;
    [NonSerialized] public PokemonPlayer lastBallHolder = null;
    private PokemonPlayer ballHolder = null;
    public PokemonPlayer BallHolder => ballHolder;
    private float lastTimeBlocked = -1f;

    public BasketTeam lastTeamHolder => lastBallHolder?.Team;
    public PokemonType lastPokemonTypeHolder => lastBallHolder?.actualPokemon.pokemonType;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.matchPlaying == false || basketBall == null)
        {
            lastTimeBlocked = -1f;
            return;
        }

        bool isStationary = basketBall.rb.linearVelocity.sqrMagnitude <= 0.0001f;
        bool isHeld = ballHolder != null;
        if (!isHeld && isStationary && basketBall.transform.position.y > 1f)
        {
            if (lastTimeBlocked == -1f)
            {
                lastTimeBlocked = Time.time;
            }
            else if (Time.time - lastTimeBlocked >= timeBeforeReset)
            {
                lastTimeBlocked = -1f;
                ResetBasketBall();
            }
        }
        else
        {
            lastTimeBlocked = -1f;
        }
    }

    public void ResetBasketBall()
    {
        Debug.LogWarning("ResetBasketBall has been called.");
        if (basketBall != null) Destroy(basketBall.gameObject);
        ballHolder = null;
        lastBallHolder = null;
        lastTimeBlocked = -1f;
        StartMatch();
    }

    public void StartMatch()
    {
        lastTimeBlocked = -1f;
        ballHolder = null;
        basketBall = Instantiate(ballPrefab, ballSpawnPoint.position, ballSpawnPoint.rotation).GetComponent<BasketBall>();
    }

    public void SetBallHolder(PokemonPlayer holder)
    {
        Debug.LogWarning("SetBallHolder has been called. ======= canBeHoldByTeam is : " + canBeHoldByTeam);
        if (holder != null)
        {
            if (canBeHoldByTeam != null && holder.Team.teamName != canBeHoldByTeam) return;
            
            ballHolder = holder;
            // Reset the rotation when we have a new holder
            canBeHoldByTeam = null;
            lastBallHolder = holder;
            
            if (holder.Team.teamName == TeamName.Blue)
            {
                GameManager.Instance.CameraManager.SetNewLookAtTransform(
                    GameManager.Instance.CameraManager.blueCameraTarget.transform,
                    new Vector3(0f, 9f, -19f)
                    );
            }
            else
            {
                GameManager.Instance.CameraManager.SetNewLookAtTransform(
                    GameManager.Instance.CameraManager.redCameraTarget.transform,
                    new Vector3(0f, 9f, -19f)
                    );
            }
        }
        else
        {
            ballHolder = holder;
        }
    }

    public void ResetHolder()
    {
        ballHolder = null;
        lastBallHolder = null;
    }

    public bool IsBallHolding()
    {
        return ballHolder != null;
    }

    public bool IsBallHolded()
    {
        return lastBallHolder != null;
    }

    public bool IsTeamHoldedBall(BasketTeam team)
    {
        return lastTeamHolder == team;
    }

    public bool IsPlayerHoldingBall(PokemonPlayer player)
    {
        return ballHolder == player;
    }

    public void ShootTo(Transform target, bool isSuccessful, float force, PokemonPlayer shooter)
    {
        SetBallHolder(null);
        basketBall.ShootTowardsBasket(target.position, isSuccessful, force, shooter);
    }

    public void PassTo(Transform target)
    {
        SetBallHolder(null);
        basketBall.PassTo(target.position);
    }
    
    public void DunkTo(Transform target)
    {
        SetBallHolder(null);
        basketBall.DunkInto(target.position);
    }
}
