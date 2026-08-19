using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    public Image loadingScreenImage;
    [SerializeField]
    private Slider loadingSlider;
    private bool _sceneIsSwapping;

    public void StartToLoadScene(int sceneToLoad){
        StartToLoadScene(sceneToLoad, (gm, spp) => {});
    }

    public void StartToLoadScene(int sceneToLoad, Action<GameManager, SelectablePokemonPanel> onEndCallback){
        if(_sceneIsSwapping == true)
            return;
        DontDestroyOnLoad(this.gameObject);
        StartCoroutine(LoadScene(sceneToLoad, onEndCallback ?? ((gm, spp) => {})));
    }

    private IEnumerator LoadScene(int sceneToLoad, Action<GameManager, SelectablePokemonPanel> onEndCallback){
        _sceneIsSwapping = true;
        float startPosition = loadingScreenImage.rectTransform.position.y;
        LeanTween.moveY(loadingScreenImage.rectTransform, 0, 1f)
            .setEase( LeanTweenType.easeOutQuart )
            .setIgnoreTimeScale(true);
        yield return new WaitForSecondsRealtime(1f); 

        AsyncOperation asyncSceneToLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneToLoad);
        if (asyncSceneToLoad == null)
        {
            Debug.LogError($"Unable to load scene with build index {sceneToLoad}.");
            Destroy(gameObject);
            yield break;
        }
        asyncSceneToLoad.allowSceneActivation = false; // stop the level from activating

        while (asyncSceneToLoad.progress < 0.9f){
            loadingSlider.value = asyncSceneToLoad.progress;
            yield return new WaitForEndOfFrame();
        } 

        loadingSlider.value = 1f;
        asyncSceneToLoad.allowSceneActivation = true; // this will enter the level now
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();
        yield return new WaitForSecondsRealtime(0.2f);
        yield return new WaitForSecondsRealtime(0.2f);
        GameManager gm = GameManager.Instance;
        SelectablePokemonPanel spp = SelectablePokemonPanel.Instance;
        try
        {
            onEndCallback?.Invoke(gm, spp);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        yield return new WaitForSecondsRealtime(0.2f);
        LeanTween.moveY(loadingScreenImage.rectTransform, -startPosition, 1f)
            .setEase( LeanTweenType.easeInQuart )
            .setIgnoreTimeScale(true);
        yield return new WaitForSecondsRealtime(1.2f);
        Destroy(this.gameObject);
        _sceneIsSwapping = false;
    }
}
