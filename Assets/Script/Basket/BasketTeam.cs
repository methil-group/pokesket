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
            if (teamScoreText != null) teamScoreText.text = value.ToString();
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
        if (pokeTeam == null || pokeTeam.Count < 3)
        {
            Debug.LogError($"Team {name} must have at least three Pokémon players.");
            return;
        }

        if (dunkLayout != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(dunkLayout.transform as RectTransform);
            if (dunkLayout.transform.childCount > 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(dunkLayout.transform.GetChild(0).transform as RectTransform);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(dunkLayout.transform as RectTransform);
        }

        if (GameManager.Instance == null || GameManager.Instance.IsTeamHumanControlled(this))
        {
            SetControlledPlayer(pokeTeam[0]);
        }
        else
        {
            controlledPlayer = null;
        }
        for (int i = 0; pokemonImages != null && i < pokeTeam.Count && i < pokemonImages.Length; i++)
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

        if (dunkBarSlider != null) dunkBarSlider.value = _dunkBar / 100f;
        if (dunkButtonImage != null) dunkButtonImage.enabled = _dunkBar == 100;
    }

    void SwitchControlledPlayer()
    {
        // Cherche le Pokémon (autre que le contrôlé) le plus proche de la balle
        PokemonPlayer nearestPokemon = pokeTeam
            .Where(p => p != controlledPlayer)
            .OrderBy(p => BasketBallManager.Instance != null && BasketBallManager.Instance.basketBall != null
                ? Vector3.Distance(p.transform.position, BasketBallManager.Instance.basketBall.transform.position)
                : float.MaxValue)
            .FirstOrDefault();

        // Si aucun trouvé (rare), on passe au suivant dans la liste
        int nextIndex = (nearestPokemon != null)
            ? pokeTeam.IndexOf(nearestPokemon)
            : (pokeTeam.IndexOf(controlledPlayer) + 1) % pokeTeam.Count;

        SetControlledPlayer(pokeTeam[nextIndex]);
    }

    public void SetControlledPlayer(PokemonPlayer newControlled)
    {
        if (newControlled == null) return;

        controlledPlayer = newControlled;
        if (uiSelectedImage != null && newControlled.actualPokemon != null)
        {
            uiSelectedImage.sprite = newControlled.actualPokemon.pokemonPortrait;
        }
        if (uiSelectedText != null && newControlled.actualPokemon != null)
        {
            uiSelectedText.text = newControlled.actualPokemon.pokemonName;
        }
        if (uiSelectedLayout == null) return;

        RectTransform layoutRect = uiSelectedLayout.GetComponent<RectTransform>();
        if (layoutRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
        if (uiSelectedLayout.transform.childCount > 1 && uiSelectedLayout.transform.GetChild(1).childCount > 0)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(uiSelectedLayout.transform.GetChild(1).GetChild(0).GetComponent<RectTransform>());
        }
        if (uiSelectedLayout.transform.childCount > 0)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(uiSelectedLayout.transform.GetChild(0).GetComponent<RectTransform>());
        }
        if (layoutRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
    }

    public bool IsControlled(PokemonPlayer player)
    {
        return player == controlledPlayer;
    }

    public BasketTeam GetOpponentTeam()
    {
        return GameManager.Instance == null ? null : GameManager.Instance.GetTeam(opponentTeamName);
    }

    public Transform GetOpponentRim()
    {
        BasketTeam opponentTeam = GetOpponentTeam();
        return opponentTeam == null ? null : opponentTeam.rim;
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
