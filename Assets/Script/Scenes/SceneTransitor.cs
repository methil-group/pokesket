using System;
using UnityEngine;

public class SceneTransitor : MonoBehaviour
{
    public static SceneTransitor Instance;

    public GameObject loadingScreen;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(int sceneToLoad)
    {
        var loadingScreenPrefab = GameObject.Instantiate(loadingScreen);
        loadingScreenPrefab.GetComponent<LoadingScreenManager>().StartToLoadScene(sceneToLoad);
    }

    public void LoadScene(int sceneToLoad, Action<GameManager, SelectablePokemonPanel> onEndCallback)
    {
        var loadingScreenPrefab = GameObject.Instantiate(loadingScreen);
        loadingScreenPrefab.GetComponent<LoadingScreenManager>().StartToLoadScene(sceneToLoad, onEndCallback);
    }

    public void LoadCharacterSelection2Players()
    {
        this.LoadScene(1, (gm, spp) =>
        { 
            spp.SetupCharacterSelectableFor2Players();
        });
    }

    public void LoadCharacterSelection1Player()
    {
        this.LoadScene(1, (gm, spp) =>
        {
            spp.SetupCharacterSelectableFor1Player();
        });
    }
}
