using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SelectablePokemonPanel : MonoBehaviour
{
    private const int TeamSize = 3;

    public static SelectablePokemonPanel Instance;
    public GameObject selectablePokemonPrefab;
    
    public Pokemon[] selectedPlayer1Characters;
    public Pokemon[] selectedPlayer2Characters;
    
    public GameObject[] selectedPlayer1CharactersPreview;
    public GameObject[] selectedPlayer2CharactersPreview;
    
    public Button startButton;
    private int gameSceneIndex = 2;
    [NonSerialized] public int maxPoint = 21;
    
    [SerializeField] private GameObject player1Cursor;
    [SerializeField] private GameObject player2Cursor;
    
    public bool is1Player = false;

    public void Start()
    {
        Instance = this;
        EnsureSelectionArrays();

        foreach (Transform child in this.transform)
        {
            Destroy(child.gameObject);
        }
        
        Pokemon[] pokemons = PokemonDatabase.Instance.pokemons;

        foreach (Pokemon pokemon in pokemons)
        {
            GameObject pokePrefab = Instantiate(selectablePokemonPrefab, this.transform);
            pokePrefab.name = "Selectable pokemon : " + pokemon.name;
            pokePrefab.GetComponentInChildren<SelectablePokemonPrefab>().Setup(pokemon, this);
        }

        SetupCharacterSelectableFor1Player();
    }

    public void SetupCharacterSelectableFor2Players()
    {
        is1Player = false;
        if (player1Cursor != null) player1Cursor.SetActive(true);
        if (player2Cursor != null) player2Cursor.SetActive(true);
        Array.Clear(selectedPlayer2Characters, 0, selectedPlayer2Characters.Length);
        UpdateCharacterPreviews();
        CheckButtonState();
    }

    public void SetupCharacterSelectableFor1Player()
    {
        is1Player = true;
        if (player1Cursor != null) player1Cursor.SetActive(true);
        if (player2Cursor != null) player2Cursor.SetActive(false);
        FillRandomPlayer2Team();
        UpdateCharacterPreviews();
        CheckButtonState();
    }

    private void Update()
    {
        if (Input.GetKeyUp(RemoteInput.B1))
        {
            RemoveLastSelectedPokemon(selectedPlayer1Characters);
        }

        if (!is1Player && Input.GetKeyUp(RemoteInput.B2))
        {
            RemoveLastSelectedPokemon(selectedPlayer2Characters);
        }
    }

    private void RemoveLastSelectedPokemon(Pokemon[] selectedCharacters)
    {
        for (int i = selectedCharacters.Length - 1; i >= 0; i--)
        {
            if (selectedCharacters[i] != null)
            {
                selectedCharacters[i] = null;
                break;
            }
        }
        RefreshPreviews();
    }

    private void UpdateCharacterPreviews()
    {
        UpdatePlayerPreviews(selectedPlayer1Characters, selectedPlayer1CharactersPreview);
        if (is1Player)
        {
            UpdatePlayerPreviews(selectedPlayer2Characters, selectedPlayer2CharactersPreview, true);
        }
        else
        {
            UpdatePlayerPreviews(selectedPlayer2Characters, selectedPlayer2CharactersPreview);
        }
    }
    
    private void UpdatePlayerPreviews(Pokemon[] selectedCharacters, GameObject[] previewObjects, bool randomized = false)
    {
        if (randomized)
        {
            for (int i = 0; i < previewObjects.Length; i++)
            {
                FadeSprite fadeSprite = previewObjects[i].GetComponent<FadeSprite>();
                if (fadeSprite != null) fadeSprite.SetRandom();
            }

            return;
        }
        
        for (int i = 0; i < previewObjects.Length; i++)
        {
            if (previewObjects[i] != null)
            {
                SpriteRenderer spriteRenderer = previewObjects[i].GetComponent<SpriteRenderer>();
                
                if (spriteRenderer != null)
                {
                    if (i < selectedCharacters.Length && selectedCharacters[i] != null)
                    {
                        FadeSprite fadeSprite = previewObjects[i].GetComponent<FadeSprite>();
                        if (fadeSprite != null)
                        {
                            fadeSprite.Show(selectedCharacters[i]);
                        }
                    }
                    else
                    {
                        FadeSprite fadeSprite = previewObjects[i].GetComponent<FadeSprite>();
                        if (fadeSprite != null)
                        {
                            fadeSprite.Hide();
                        }
                    }
                }
            }
        }
    }
    
    private void CheckButtonState()
    {
        if (startButton != null)
            startButton.interactable = EveryoneSelected();
    }

    public bool EveryoneSelected()
    {
        if (!AreAllCharactersSelected(selectedPlayer1Characters)) return false;
        return is1Player || AreAllCharactersSelected(selectedPlayer2Characters);
    }
    
    public void RefreshPreviews()
    {
        UpdateCharacterPreviews();
        CheckButtonState();
    }

    public void LaunchGame()
    {
        if (EveryoneSelected())
        {
            SceneTransitor.Instance.LoadScene(gameSceneIndex, (gm, spp) =>
            {
                if (gm == null)
                {
                    Debug.LogWarning("Error getting game manager");
                    return;
                }

                gm.StartMatch(
                    selectedPlayer1Characters.ToList(),
                    selectedPlayer2Characters.ToList(),
                    maxPoint,
                    is1Player
                );
            });
        }
    }
    
    public void SelectPokemonForPlayer1(int slot, Pokemon pokemon)
    {
        if (slot >= 0 && slot < selectedPlayer1Characters.Length)
        {
            selectedPlayer1Characters[slot] = pokemon;
            UpdateCharacterPreviews();
            CheckButtonState();
        }
    }
    
    public void SelectPokemonForPlayer2(int slot, Pokemon pokemon)
    {
        if (!is1Player && slot >= 0 && slot < selectedPlayer2Characters.Length)
        {
            selectedPlayer2Characters[slot] = pokemon;
            UpdateCharacterPreviews();
            CheckButtonState();
        }
    }

    public void ReturnFirstScene()
    {
        SceneTransitor.Instance.LoadScene(0);
    }

    private void EnsureSelectionArrays()
    {
        selectedPlayer1Characters = ResizeSelectionArray(selectedPlayer1Characters);
        selectedPlayer2Characters = ResizeSelectionArray(selectedPlayer2Characters);
    }

    private static Pokemon[] ResizeSelectionArray(Pokemon[] characters)
    {
        if (characters != null && characters.Length == TeamSize) return characters;

        Pokemon[] resizedCharacters = new Pokemon[TeamSize];
        if (characters != null)
        {
            Array.Copy(characters, resizedCharacters, Mathf.Min(characters.Length, TeamSize));
        }
        return resizedCharacters;
    }

    private void FillRandomPlayer2Team()
    {
        Pokemon[] availablePokemons = PokemonDatabase.Instance?.pokemons;
        if (availablePokemons == null || availablePokemons.Length == 0) return;

        Pokemon[] randomizedPokemons = availablePokemons
            .Where(pokemon => pokemon != null)
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(TeamSize)
            .ToArray();

        Array.Clear(selectedPlayer2Characters, 0, selectedPlayer2Characters.Length);
        for (int i = 0; i < randomizedPokemons.Length; i++)
        {
            selectedPlayer2Characters[i] = randomizedPokemons[i];
        }
    }

    private static bool AreAllCharactersSelected(Pokemon[] characters)
    {
        if (characters == null || characters.Length != TeamSize) return false;
        return characters.All(pokemon => pokemon != null);
    }
}
