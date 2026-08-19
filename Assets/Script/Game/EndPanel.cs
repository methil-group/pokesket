using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine;

public class EndPanel : MonoBehaviour
{
    public static EndPanel Instance;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TextMeshProUGUI teamWinText;
    [SerializeField] private TextMeshProUGUI mvpNameText;
    [SerializeField] private TextMeshProUGUI mvpPointNumberText;
    [SerializeField] private Canvas canvas;

    private Vector3 hiddenPosition;
    private Vector3 centerPosition;

    private bool isEndMenuActive = false;
    private Coroutine _returnToSelectionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        RectTransform panelRect = endPanel.GetComponent<RectTransform>();

        float offScreenY = canvasRect.rect.height / 2 + panelRect.rect.height / 2;
        hiddenPosition = new Vector3(0, offScreenY, 0);
        centerPosition = Vector3.zero;

        endPanel.GetComponent<RectTransform>().anchoredPosition = hiddenPosition;
    }

    public void ShowWin(BasketTeam team)
    {
        if (isEndMenuActive == true) return;
        if (team == null || team.pokeTeam == null || team.pokeTeam.Count == 0)
        {
            Debug.LogError("Cannot show the end panel without a winning team.");
            return;
        }

        isEndMenuActive = true;
        teamWinText.text = team.teamName.ToString().ToUpper() + " TEAM";
        var mvpPokemon = team.pokeTeam.OrderByDescending(player => player.pointScored).First();
        mvpNameText.text = mvpPokemon.actualPokemon.pokemonName;
        mvpPointNumberText.text = mvpPokemon.pointScored.ToString() + " Points"; 
        GameManager.Instance.CameraManager.SetNewLookAtTransform(mvpPokemon.transform, new Vector3(0, 5, -8), new Vector3(0, 0.1f));
        LeanTween.move(endPanel.GetComponent<RectTransform>(), centerPosition, 1.2f).setEaseOutSine();
        _returnToSelectionCoroutine = StartCoroutine(ReturnToSelectionAfterDelay());
    }

    private IEnumerator ReturnToSelectionAfterDelay()
    {
        yield return new WaitForSecondsRealtime(10f);
        if (SceneTransitor.Instance != null)
        {
            SceneTransitor.Instance.LoadScene(1);
        }
    }

    private void OnDestroy()
    {
        if (_returnToSelectionCoroutine != null)
        {
            StopCoroutine(_returnToSelectionCoroutine);
        }

        if (Instance == this) Instance = null;
    }
}
