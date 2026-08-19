using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PokemonPlayer))]
public class ShootPlayer : MonoBehaviour
{
    [Header("Reference")]
    public GameObject shootingUi;
    public Slider shootingSlider;
    public Image shootingSliderFill;
    public Slider overLoadSlider;

    [Header("Feedback UI")]
    [SerializeField] private GameObject feedbackTooEarly;
    [SerializeField] private GameObject feedbackGreat;
    [SerializeField] private GameObject feedbackGood;
    [SerializeField] private GameObject feedbackPerfect;
    [SerializeField] private GameObject feedbackTooLate;

    private PokemonPlayer _pokemonPlayer;

    private float speedNerf = 0.3f;
    private bool isShootingMode = false;
    private float shootingTimer = 0f;
    private float currentShootingWindow;
    private float _currentCursorPosition = 0f;
    private float _timeAt100Percent = 0f;
    private const float MAX_TIME_AT_100 = 0.2f;
    private bool _shotButtonConsumed = false;
    private bool _speedWasReduced = false;
    private float _speedBeforeShooting;

    private float DistanceFactor()
    {
        float distance = Vector3.Distance(_pokemonPlayer.transform.position, _pokemonPlayer.Team.GetOpponentRim().position);

        float minDistance = 3f;
        float maxDistance = 15f;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // Logarithmique de base
        float logMin = Mathf.Log(minDistance);
        float logMax = Mathf.Log(maxDistance);
        float currentLog = Mathf.Log(distance);

        float baseFactor = Mathf.InverseLerp(logMin, logMax, currentLog);

        // Accentuer la courbe (exponentielle inverse)
        return Mathf.Pow(baseFactor, 2f);
    }

    private float perfectThreshold => Mathf.Lerp(0.80f, 0.95f, DistanceFactor());
    private float goodThreshold => perfectThreshold - 0.10f;
    private float okThreshold => goodThreshold - 0.10f;

    private Vector3 _originalSliderPosition;
    private CanvasGroup _sliderCanvasGroup;

    private int perfectDunkPoints = 30;
    private int goodDunkPoints = 15;
    private int okDunkPoints = 5;

    void Awake()
    {
        _pokemonPlayer = GetComponent<PokemonPlayer>();
        shootingUi.gameObject.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.matchPlaying == false) return;
        
        if (_pokemonPlayer.isBlockingPass || _pokemonPlayer.isBlockingShoot) return;

        KeyCode shotButton = _pokemonPlayer.ControlledByPlayer1 ? RemoteInput.B1 : RemoteInput.B2;
        if (!isShootingMode && !Input.GetKey(shotButton))
        {
            _shotButtonConsumed = false;
        }

        if (!_pokemonPlayer.IsControlled || !_pokemonPlayer.HasBall)
        {
            if (isShootingMode) CancelShootingMode();
            return;
        }

        if (Input.GetKeyUp(shotButton))
        {
            if (_shotButtonConsumed)
            {
                _shotButtonConsumed = false;
            }
            else if (isShootingMode)
            {
                ExecuteShot();
            }
        }

        if (_shotButtonConsumed) return;

        if (Input.GetKey(shotButton))
        {
            if (!isShootingMode)
            {
                StartShootingMode();
            }
            else
            {
                UpdateShootingMode();
            }
        }
        if (Input.GetKeyDown(_pokemonPlayer.ControlledByPlayer1 ? RemoteInput.A1 : RemoteInput.A2))
        {
            if (isShootingMode)
            {
                CancelShootingMode();
            }
        }
    }

    private void StartShootingMode()
    {
        if (!_pokemonPlayer.canShoot || !_pokemonPlayer.IsControlled || !_pokemonPlayer.HasBall) return;

        isShootingMode = true;
        shootingTimer = 0f;
        _timeAt100Percent = 0f;

        currentShootingWindow = Mathf.Lerp(1.3f, 0.6f, _pokemonPlayer.shootPrecision / 100f);

        SetupJuiceEffects();

        shootingUi.SetActive(true);
        shootingSlider.value = 0f;
        overLoadSlider.value = 0f;

        _speedBeforeShooting = _pokemonPlayer.speed;
        _speedWasReduced = true;
        _pokemonPlayer.speed *= speedNerf;
        StartCoroutine(FadeInSlider());
    }

    private void UpdateShootingMode()
    {
        shootingTimer += Time.deltaTime;
        float cycleSpeed = 2f / currentShootingWindow;
        float cursorPosition = shootingTimer * cycleSpeed;

        if (cursorPosition >= 1f)
        {
            _timeAt100Percent += Time.deltaTime;
            if (_timeAt100Percent >= MAX_TIME_AT_100)
            {
                ExecuteShot();
                return;
            }
        }
        else
        {
            _timeAt100Percent = 0f;
        }

        shootingSlider.value = Mathf.Min(1f, cursorPosition);
        overLoadSlider.value = Mathf.Max(0f, cursorPosition - 1f);

        if (cursorPosition > 1f)
        {
            shootingSliderFill.color = Color.red;
        }
        else if (cursorPosition >= perfectThreshold && cursorPosition <= 1f)
        {
            shootingSliderFill.color = Color.green;
        }
        else if (cursorPosition >= goodThreshold)
        {
            shootingSliderFill.color = Color.yellow;
        }
        else if (cursorPosition >= okThreshold)
        {
            shootingSliderFill.color = new Color(1f, 0.5f, 0f);
        }
        else
        {
            shootingSliderFill.color = Color.red;
        }

        _currentCursorPosition = cursorPosition;
    }

    private void ExecuteShot()
    {
        if (!isShootingMode) return;

        isShootingMode = false;
        _shotButtonConsumed = true;

        if (!_pokemonPlayer.IsControlled || !_pokemonPlayer.HasBall)
        {
            StartCoroutine(SimpleFadeOut());
            return;
        }

        float shootingQuality = CalculateShootingQuality(_currentCursorPosition);
        if (_timeAt100Percent >= MAX_TIME_AT_100)
            shootingQuality = 0f;

        var rim = _pokemonPlayer.Team.GetOpponentRim();
        _pokemonPlayer.ShootBall();

        PlayShootAnimation();

        bool guaranteedHit = _currentCursorPosition >= perfectThreshold && _currentCursorPosition <= 1f;

        float force;
        if (guaranteedHit)
        {
            force = 1f;
        }
        else if (_currentCursorPosition < perfectThreshold)
        {
            force = Mathf.Lerp(0.5f, 0.9f, _currentCursorPosition / perfectThreshold);
        }
        else
        {
            force = Mathf.Lerp(1.1f, 1.5f, Mathf.Clamp((_currentCursorPosition - 1f), 0f, 1f));
        }

        BasketBallManager.Instance.ShootTo(rim, guaranteedHit, force, _pokemonPlayer);

        IncreaseDunkBar(_currentCursorPosition);

        ShowShotFeedback(_currentCursorPosition);
        StartCoroutine(ShakeAndFadeOut(shootingQuality));
    }

    private void PlayShootAnimation()
    {
        var move = _pokemonPlayer.Direction;
        if (move.magnitude == 0)
        {
            _pokemonPlayer.pokemonPlayerAnimator.PlayOneShotAnimation(_pokemonPlayer.actualPokemon.shootTopRightAnimation);
            return;
        }
        if (move.x > 0)
            _pokemonPlayer.pokemonPlayerAnimator.PlayOneShotAnimation(move.z > 0 ? _pokemonPlayer.actualPokemon.shootTopRightAnimation : _pokemonPlayer.actualPokemon.shootBottomRightAnimation);
        else if (move.x < 0)
            _pokemonPlayer.pokemonPlayerAnimator.PlayOneShotAnimation(move.z > 0 ? _pokemonPlayer.actualPokemon.shootTopLeftAnimation : _pokemonPlayer.actualPokemon.shootBottomLeftAnimation);
        else
            _pokemonPlayer.pokemonPlayerAnimator.PlayOneShotAnimation(move.z > 0 ? _pokemonPlayer.actualPokemon.shootTopLeftAnimation : _pokemonPlayer.actualPokemon.bottomLeftAnimation);
    }

    private float CalculateShootingQuality(float cursorPosition)
    {
        if (cursorPosition >= perfectThreshold)
        {
            float distanceFrom100 = Mathf.Abs(1f - cursorPosition);
            return Mathf.Lerp(1.3f, 1.2f, distanceFrom100 * 20f);
        }
        else if (cursorPosition >= goodThreshold)
        {
            float normalizedInRange = (cursorPosition - goodThreshold) / (perfectThreshold - goodThreshold);
            return Mathf.Lerp(1.0f, 1.2f, normalizedInRange);
        }
        else if (cursorPosition >= okThreshold)
        {
            float normalizedInRange = (cursorPosition - okThreshold) / (goodThreshold - okThreshold);
            return Mathf.Lerp(0.7f, 1.0f, normalizedInRange);
        }
        else
        {
            float normalizedPosition = cursorPosition / okThreshold;
            return Mathf.Lerp(0.1f, 0.7f, normalizedPosition);
        }
    }

    private void ShowShotFeedback(float cursorPosition)
    {
        string feedbackText;
        if (cursorPosition > 1f)
        {
            feedbackText = "TOO LATE!";
            StartCoroutine(ShowJuicyFeedback(feedbackTooLate));
        }
        else if (cursorPosition >= perfectThreshold && cursorPosition <= 1f)
        {
            feedbackText = "PERFECT SHOT!";
            StartCoroutine(ShowJuicyFeedback(feedbackPerfect));
        }
        else if (cursorPosition >= goodThreshold)
        {
            feedbackText = "GREAT SHOT!";
            StartCoroutine(ShowJuicyFeedback(feedbackGreat));
        }
        else if (cursorPosition >= okThreshold)
        {
            feedbackText = "Good Shot";
            StartCoroutine(ShowJuicyFeedback(feedbackGood));
        }
        else
        {
            feedbackText = "Off Target";
            StartCoroutine(ShowJuicyFeedback(feedbackTooEarly));
        }

        Debug.Log($"Shot Result: {feedbackText} | Cursor Position: {cursorPosition:P1}");
    }

    private void IncreaseDunkBar(float cursorPosition)
    {
        if (cursorPosition > 1f)
        {
            _pokemonPlayer.Team.IncreaseDunkBar(0);
        }
        if (cursorPosition >= perfectThreshold && cursorPosition <= 1f)
        {
            _pokemonPlayer.Team.IncreaseDunkBar(perfectDunkPoints);
        }
        else if (cursorPosition >= goodThreshold)
        {
            _pokemonPlayer.Team.IncreaseDunkBar(goodDunkPoints);
        }
        else if (cursorPosition >= okThreshold)
        {
            _pokemonPlayer.Team.IncreaseDunkBar(okDunkPoints);
        }
        else
        {
            _pokemonPlayer.Team.IncreaseDunkBar(0);
        }
    }

    private void CancelShootingMode()
    {
        if (!isShootingMode) return;

        isShootingMode = false;
        _shotButtonConsumed = true;
        StartCoroutine(SimpleFadeOut());
    }

    private void SetupJuiceEffects()
    {
        _sliderCanvasGroup = shootingUi.GetComponent<CanvasGroup>();
        if (_sliderCanvasGroup == null)
            _sliderCanvasGroup = shootingUi.AddComponent<CanvasGroup>();

        _originalSliderPosition = shootingUi.transform.localPosition;
        _sliderCanvasGroup.alpha = 0f;
    }

    private IEnumerator FadeInSlider()
    {
        float duration = 0.3f, elapsed = 0f;
        shootingUi.transform.localScale = Vector3.one * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            _sliderCanvasGroup.alpha = progress;

            float scale = Mathf.Lerp(0.5f, 1.2f, progress);
            if (progress > 0.7f) scale = Mathf.Lerp(1.2f, 1f, (progress - 0.7f) / 0.3f);
            shootingUi.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        _sliderCanvasGroup.alpha = 1f;
        shootingUi.transform.localScale = Vector3.one;
    }

    private IEnumerator ShakeAndFadeOut(float shootingQuality)
    {
        float shakeIntensity = 20f, shakeDuration = 0.2f;
        if (shootingQuality >= 1.0f) { shakeIntensity = 30f; shakeDuration = 0.25f; }
        else if (shootingQuality >= 0.7f) { shakeIntensity = 40f; shakeDuration = 0.3f; }
        else if (shootingQuality >= 0.4f) { shakeIntensity = 60f; shakeDuration = 0.4f; }
        else { shakeIntensity = 100f; shakeDuration = 0.5f; }

        float shakeElapsed = 0f;
        while (shakeElapsed < shakeDuration)
        {
            shakeElapsed += Time.deltaTime;
            Vector3 shakeOffset = new Vector3(Random.Range(-shakeIntensity, shakeIntensity), Random.Range(-shakeIntensity, shakeIntensity), 0f);
            shootingUi.transform.localPosition = _originalSliderPosition + shakeOffset;
            yield return null;
        }

        shootingUi.transform.localPosition = _originalSliderPosition;

        float fadeDuration = 0.4f, fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float progress = fadeElapsed / fadeDuration;
            _sliderCanvasGroup.alpha = 1f - progress;
            shootingUi.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, progress);
            yield return null;
        }

        FinishShootingMode();
    }

    private IEnumerator SimpleFadeOut()
    {
        float fadeDuration = 0.3f, elapsed = 0f;
        float startAlpha = _sliderCanvasGroup != null ? _sliderCanvasGroup.alpha : 1f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            _sliderCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            shootingUi.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, progress);
            yield return null;
        }

        FinishShootingMode();
    }

    private void FinishShootingMode()
    {
        isShootingMode = false;
        _timeAt100Percent = 0f;
        if (_speedWasReduced)
        {
            _pokemonPlayer.speed = _speedBeforeShooting;
            _speedWasReduced = false;
        }
        shootingUi.SetActive(false);
        shootingUi.transform.localPosition = _originalSliderPosition;
        shootingUi.transform.localScale = Vector3.one;
        if (_sliderCanvasGroup != null) _sliderCanvasGroup.alpha = 1f;
    }

    private IEnumerator ShowJuicyFeedback(GameObject feedbackGO)
    {
        CanvasGroup canvas = feedbackGO.GetComponent<CanvasGroup>();
        if (canvas == null)
            canvas = feedbackGO.AddComponent<CanvasGroup>();

        feedbackGO.SetActive(true);
        canvas.alpha = 0f;
        feedbackGO.transform.localScale = Vector3.one * 0.3f;

        // Fade in + scale
        float durationIn = 0.25f;
        float elapsed = 0f;
        while (elapsed < durationIn)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / durationIn;
            canvas.alpha = t;
            feedbackGO.transform.localScale = Vector3.Lerp(Vector3.one * 0.3f, Vector3.one * 1.2f, t);
            yield return null;
        }

        // Squeeze
        feedbackGO.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(0.7f);

        // Fade out
        float durationOut = 0.25f;
        elapsed = 0f;
        while (elapsed < durationOut)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / durationOut;
            canvas.alpha = 1f - t;
            feedbackGO.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.3f, t);
            yield return null;
        }

        feedbackGO.SetActive(false);
    }
}
