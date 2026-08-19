using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PokemonPlayer))]
[RequireComponent(typeof(Zone2PtsDetector))]
public class DunkPlayer : MonoBehaviour
{
    [Header("Dunk Settings")]
    public float dunkRange = 10f;
    public GameObject dunkFeedbackPrefab;

    private PokemonPlayer _pokemonPlayer;
    private Zone2PtsDetector _zoneDetector;

    void Awake()
    {
        _pokemonPlayer = GetComponent<PokemonPlayer>();
        _zoneDetector = GetComponent<Zone2PtsDetector>();
    }

    void Update()
    {
        if (GameManager.Instance.matchPlaying == false) return;
        
        if (_pokemonPlayer.isBlockingPass || _pokemonPlayer.isBlockingShoot) return;

        if (Input.GetKeyDown(_pokemonPlayer.ControlledByPlayer1 ? RemoteInput.Y1 : RemoteInput.Y2))
        {
            TryDunk();
        }
    }

    void TryDunk()
    {
        if (!_pokemonPlayer.canDunk || !_pokemonPlayer.Team.canDunk || !_pokemonPlayer.IsControlled || !_pokemonPlayer.HasBall) return;

        Transform rim = _pokemonPlayer.Team.GetOpponentRim();
        Vector3 rimPosition = rim.position;
        rimPosition.y = transform.position.y;

        float distanceToRim = Vector3.Distance(transform.position, rimPosition);

        bool inCorrectZone = _zoneDetector.IsInOpponent2PtsZone(_pokemonPlayer.Team.teamName);

        if (distanceToRim <= dunkRange && inCorrectZone)
        {
            ExecuteDunk(rim);
        }
        else
        {
            Debug.Log($"Dunk refusé - Distance OK ? {distanceToRim <= dunkRange}, Zone OK ? {inCorrectZone}");
        }
    }

    void ExecuteDunk(Transform rim)
    {
        StartCoroutine(PlayDunkAnimation(rim));
        _pokemonPlayer.Team.ResetDunkBar();
        Debug.Log("🔥 DUNK lancé !");
    }

    IEnumerator PlayDunkAnimation(Transform rim)
    {
        // Sauvegarder la hauteur d’origine
        float groundY = _pokemonPlayer.transform.position.y;
        _pokemonPlayer.DunkBall();

        // Le suivi de la balle est déjà automatique grâce à BallHolder
        // Pas besoin de manipuler transform.position de la balle

        Vector3 start = _pokemonPlayer.transform.position;
        Vector3 towardRim = (rim.position - _pokemonPlayer.transform.position).normalized;
        Vector3 target = rim.position - towardRim * 0.4f + Vector3.up * 0.7f;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            _pokemonPlayer.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        BasketBallManager.Instance.DunkTo(rim);

        yield return new WaitForSeconds(0.05f); // petit délai de transition

        // Descente progressive du joueur
        float descentDuration = 0.3f;
        float descentElapsed = 0f;
        Vector3 descentStart = _pokemonPlayer.transform.position;
        Vector3 descentEnd = new Vector3(descentStart.x, groundY, descentStart.z);

        while (descentElapsed < descentDuration)
        {
            descentElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, descentElapsed / descentDuration);
            _pokemonPlayer.transform.position = Vector3.Lerp(descentStart, descentEnd, t);
            yield return null;
        }

        // Snap final au sol
        Vector3 finalPos = _pokemonPlayer.transform.position;
        finalPos.y = groundY;
        _pokemonPlayer.transform.position = finalPos;
    }
}
