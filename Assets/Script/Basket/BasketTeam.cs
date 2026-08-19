using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TeamName { Blue, Red }

public class BasketTeam : MonoBehaviour
{
    // Base info
    [SerializeField] public TeamName teamName;
    [SerializeField] public Transform rim;
    [SerializeField] private Image uiSelectedImage;
    [SerializeField] private TextMeshProUGUI uiSelectedText;
    [SerializeField] private LayoutGroup uiSelectedLayout;

    // Player pokemon part
    public List<PokemonPlayer> pokeTeam;
    public Image[] pokemonImages;
    public TeamName opponentTeamName => teamName == TeamName.Blue ? TeamName.Red : TeamName.Blue;
    [NonSerialized] public PokemonPlayer controlledPlayer;

    // Tactic part
    public GameObject TopZone;
    public GameObject FrontZone;
    public GameObject BottomZone;

    // Score part
    [SerializeField] private TextMeshProUGUI teamScoreText;
    private int _teamScore = 0;
    public int teamScore
    {
        get => _teamScore;
        set
        {
            // Whenever we set team score, it update the TextMeshPro for the score
            teamScoreText.text = value.ToString();
            _teamScore = value;
        }
    }

    // Dunk part
    private int _dunkBar = 0;
    [SerializeField] private LayoutGroup dunkLayout;
    [SerializeField] private Slider dunkBarSlider;
    [SerializeField] private Image dunkButtonImage;
    public bool canDunk => _dunkBar == 100;

    public void StartMatch()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(dunkLayout.transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(dunkLayout.transform.GetChild(0).transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(dunkLayout.transform as RectTransform);
        if (GameManager.Instance == null || GameManager.Instance.IsTeamHumanControlled(this))
        {
            SetControlledPlayer(pokeTeam[0]);
        }
        else
        {
            controlledPlayer = null;
        }
        for (int i = 0; i < pokeTeam.Count; i++)
        {
            pokemonImages[i].sprite = pokeTeam[i].actualPokemon.pokemonPortrait;
        }
        pokeTeam[0].role = PokemonRole.Front;
        pokeTeam[1].role = PokemonRole.Top;
        pokeTeam[2].role = PokemonRole.Bottom;
    }

    void Update()
    {
        if (controlledPlayer != null)
        {
            if (Input.GetKeyDown(controlledPlayer.ControlledByPlayer1 ? RemoteInput.RB1 : RemoteInput.RB2)) // RB on Xbox
            {
                if (!controlledPlayer?.HasBall ?? false)
                {
                    SwitchControlledPlayer();
                }
            }
        }

        dunkBarSlider.value = _dunkBar / 100f;
        dunkButtonImage.enabled = _dunkBar == 100;
    }

    void SwitchControlledPlayer()
    {
        // Cherche le Pokémon (autre que le contrôlé) le plus proche de la balle
        PokemonPlayer nearestPokemon = pokeTeam
            .Where(p => p != controlledPlayer)
            .OrderBy(p => Vector3.Distance(p.transform.position, BasketBallManager.Instance.basketBall.transform.position))
            .FirstOrDefault();

        // Si aucun trouvé (rare), on passe au suivant dans la liste
        int nextIndex = (nearestPokemon != null)
            ? pokeTeam.IndexOf(nearestPokemon)
            : (pokeTeam.IndexOf(controlledPlayer) + 1) % pokeTeam.Count;

        SetControlledPlayer(pokeTeam[nextIndex]);
    }

    public void SetControlledPlayer(PokemonPlayer newControlled)
    {
        controlledPlayer = newControlled;
        Image image = uiSelectedImage;
        TextMeshProUGUI text = uiSelectedText;
        image.sprite = newControlled.actualPokemon.pokemonPortrait;
        text.text = newControlled.actualPokemon.pokemonName;
        LayoutRebuilder.ForceRebuildLayoutImmediate(uiSelectedLayout.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(uiSelectedLayout.transform.GetChild(1).GetChild(0).GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(uiSelectedLayout.transform.GetChild(0).GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(uiSelectedLayout.GetComponent<RectTransform>());
    }

    public bool IsControlled(PokemonPlayer player)
    {
        return player == controlledPlayer;
    }

    public BasketTeam GetOpponentTeam()
    {
        return GameManager.Instance.GetTeam(opponentTeamName);
    }

    public Transform GetOpponentRim()
    {
        return GetOpponentTeam().rim;
    }

    public void ResetDunkBar()
    {
        _dunkBar = 0;
    }

    public void IncreaseDunkBar(int amount)
    {
        _dunkBar += amount;
        _dunkBar = Mathf.Max(Mathf.Min(_dunkBar, 100), 0);
    }
}
