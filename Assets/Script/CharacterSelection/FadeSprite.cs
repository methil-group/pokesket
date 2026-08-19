using UnityEngine;
using UnityEngine.UI;

public class FadeSprite : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Image typeImage;
    public Image pokemonPosition;
    public float fadeSpeed = 5f;

    private float targetFade = 1f;
    private float currentFade = 0f;

    private bool isActive = false;
    private Pokemon _pokemon;

    private bool showRandom = false;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        spriteRenderer.material = new Material(spriteRenderer.material);   
    }

    private void Update()
    {
        if (showRandom && spriteRenderer != null)
        {
            PokemonDatabase database = PokemonDatabase.Instance;
            if (database == null) return;

            spriteRenderer.material.SetFloat("_Fade", 1f);
            spriteRenderer.sprite = database.randomPokemonSprite;
            if (typeImage != null) typeImage.sprite = database.randomPokemonType;
            return;
        }
        
        if (spriteRenderer != null)
        {
            currentFade = Mathf.Lerp(currentFade, targetFade, Time.deltaTime * fadeSpeed);
            spriteRenderer.material.SetFloat("_Fade", currentFade);

            if (currentFade <= 0.01f && !isActive)
            {
                spriteRenderer.sprite = null; // On cache vraiment le sprite à la fin du fade-out
            }
        }
    }

    public void Show(Pokemon pokemon)
    {
        if (pokemon == null || spriteRenderer == null) return;

        showRandom = false;
        _pokemon = pokemon;
        if (pokemonPosition != null) pokemonPosition.gameObject.SetActive(true);
        spriteRenderer.sprite = pokemon.pokemonSprite;
        spriteRenderer.material.SetColor("_GlowColor" ,pokemon.pokemonType.typeColor);
        if (typeImage != null)
        {
            typeImage.sprite = pokemon.pokemonType.typeIcon;
            typeImage.color = new Color(1f, 1f, 1f, 1f);
        }
        targetFade = 1f;
        isActive = true;
    }

    public void Hide()
    {
        showRandom = false;
        _pokemon = null;
        if (pokemonPosition != null) pokemonPosition.gameObject.SetActive(false);
        if (typeImage != null)
        {
            typeImage.sprite = null;
            typeImage.color = new Color(1f, 1f, 1f, 0f);
        }
        targetFade = 0f;
        isActive = false;
    }

    public void SetRandom()
    {
        showRandom = true;
        if (pokemonPosition != null) pokemonPosition.gameObject.SetActive(true);
        if (typeImage != null) typeImage.color = new Color(1f, 1f, 1f, 1f);
    }
}
