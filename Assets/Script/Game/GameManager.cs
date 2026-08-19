using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const int PlayersPerTeam = 3;

    public static GameManager Instance;
    [SerializeField] private BasketTeam[] teams;
    public bool matchPlaying = false;
    public bool IsSinglePlayer { get; private set; }
    private bool _matchEnded;

    public int maxPoint = 21;
    public bool IsMatchEnd
    {
        get
        {
            if (teams == null) return false;

            foreach (BasketTeam team in teams)
            {
                if (team != null && team.teamScore >= maxPoint)
                {
                    return true;
                }
            }
            return false;
        }
    }
    
    [SerializeField]
    public CameraManager CameraManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("Destroying GameManager, instance already exists.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (CameraManager != null) CameraManager.Start();
        else Debug.LogError("GameManager has no CameraManager configured.");
    }

    void Update()
    {
        if (!_matchEnded && IsMatchEnd)
        {
            EndMatch();
        }
    }

#if UNITY_EDITOR
    private bool IsLaunchedDirectly()
    {
        return SceneManager.sceneCount == 1;
    }
#endif

    public void StartMatch(List<Pokemon> pokeTeamBlue, List<Pokemon> pokeTeamRed, int _maxPoint = 2, bool _isSinglePlayer = false)
    {
        if (!ValidateMatchSetup(pokeTeamBlue, pokeTeamRed)) return;

        matchPlaying = false;
        _matchEnded = false;
        IsSinglePlayer = _isSinglePlayer;
        if (!BasketBallManager.Instance.StartMatch()) return;
        for (int i = 0; i < pokeTeamBlue.Count; i++)
        {
            var pokemon = pokeTeamBlue[i];
            teams[0].pokeTeam[i].Team = teams[0];
            teams[0].pokeTeam[i].Setup(pokemon);
        }

        for (int i = 0; i < pokeTeamRed.Count; i++)
        {
            var pokemon = pokeTeamRed[i];
            teams[1].pokeTeam[i].Team = teams[1];
            teams[1].pokeTeam[i].Setup(pokemon);
        }

        foreach (BasketTeam team in teams)
        {
            team.teamScore = 0;
            team.ResetDunkBar();
            team.StartMatch();
            team.rim.parent.GetComponent<BasketRim>().netRimCloth.sphereColliders = new[]
            {
                new ClothSphereColliderPair(BasketBallManager.Instance.basketBall.GetComponent<SphereCollider>())
            };
        }
        maxPoint = Mathf.Max(1, _maxPoint);
        matchPlaying = true;
    }

    private bool ValidateMatchSetup(List<Pokemon> pokeTeamBlue, List<Pokemon> pokeTeamRed)
    {
        if (CameraManager == null || BasketBallManager.Instance == null)
        {
            Debug.LogError("Cannot start match: a required game manager is missing.");
            return false;
        }

        if (teams == null || teams.Length < 2 || teams[0] == null || teams[1] == null)
        {
            Debug.LogError("Cannot start match: both teams must be configured.");
            return false;
        }

        if (!IsValidPokemonTeam(pokeTeamBlue) || !IsValidPokemonTeam(pokeTeamRed))
        {
            Debug.LogError($"Cannot start match: each selected team must contain exactly {PlayersPerTeam} Pokémon.");
            return false;
        }

        for (int i = 0; i < 2; i++)
        {
            BasketTeam team = teams[i];
            if (team.pokeTeam == null || team.pokeTeam.Count != PlayersPerTeam)
            {
                Debug.LogError($"Cannot start match: team {team.name} must contain exactly {PlayersPerTeam} player objects.");
                return false;
            }

            BasketRim basketRim = team.rim == null || team.rim.parent == null
                ? null
                : team.rim.parent.GetComponent<BasketRim>();
            if (basketRim == null || basketRim.netRimCloth == null)
            {
                Debug.LogError($"Cannot start match: team {team.name} has no valid rim configuration.");
                return false;
            }
        }

        return true;
    }

    private static bool IsValidPokemonTeam(List<Pokemon> team)
    {
        return team != null && team.Count == PlayersPerTeam && team.All(pokemon => pokemon != null);
    }

    public BasketTeam GetTeam(TeamName teamName)
    {
        int teamIndex = (int)teamName;
        if (teams == null || teamIndex < 0 || teamIndex >= teams.Length)
        {
            Debug.LogError($"Team {teamName} is not configured.");
            return null;
        }

        return teams[teamIndex];
    }

    public bool IsTeamHumanControlled(BasketTeam team)
    {
        return !IsSinglePlayer || team != null && team.teamName == TeamName.Blue;
    }

    public void EndMatch()
    {
        if (_matchEnded) return;

        if (teams == null)
        {
            Debug.LogError("Cannot end match: teams are not configured.");
            _matchEnded = true;
            matchPlaying = false;
            return;
        }

        _matchEnded = true;
        matchPlaying = false;
        BasketTeam winningTeam = teams.ToList().Find(team => team.teamScore >= maxPoint);
        if (EndPanel.Instance != null && winningTeam != null)
        {
            EndPanel.Instance.ShowWin(winningTeam);
        }
        else
        {
            Debug.LogError("Cannot display match end: no winning team or end panel configured.");
        }
    }
}
