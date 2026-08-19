using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
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
            foreach (BasketTeam team in teams)
            {
                if (team.teamScore >= maxPoint)
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
        _matchEnded = false;
        IsSinglePlayer = _isSinglePlayer;
        BasketBallManager.Instance.StartMatch();
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
            team.StartMatch();
            team.rim.parent.GetComponent<BasketRim>().netRimCloth.sphereColliders = new[]
            {
                new ClothSphereColliderPair(BasketBallManager.Instance.basketBall.GetComponent<SphereCollider>())
            };
        }
        maxPoint = _maxPoint;
        matchPlaying = true;
    }

    public BasketTeam GetTeam(TeamName teamName)
    {
        return teams[(int)teamName];
    }

    public bool IsTeamHumanControlled(BasketTeam team)
    {
        return !IsSinglePlayer || team != null && team.teamName == TeamName.Blue;
    }

    public void EndMatch()
    {
        if (_matchEnded) return;

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
