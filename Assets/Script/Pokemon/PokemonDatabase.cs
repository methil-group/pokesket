using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu(fileName = "PokemonDatabase", menuName = "Pokemon/PokemonDatabase")]
public class PokemonDatabase : ScriptableObject
{
    private static PokemonDatabase _instance;

    public static PokemonDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<PokemonDatabase>("PokemonDatabase");
                if (_instance == null)
                {
                    Debug.LogError("PokemonDatabase instance not found in Resources folder!");
                    return null;
                }
            }
            _instance.AssignUniqueIDs();
            return _instance;
        }
    }

    public Pokemon[] pokemons;
    
    public Sprite randomPokemonSprite;
    public Sprite randomPokemonType;

    private void AssignUniqueIDs()
    {
        if (pokemons != null)
        {
            for (int i = 0; i < pokemons.Length; i++)
            {
                if (pokemons[i] != null) pokemons[i].id = i;
            }
        }
    }


#if UNITY_EDITOR
    [ContextMenu("Refresh Pokemon List")]
    public void RefreshPokemon()
    {
        string[] guids = AssetDatabase.FindAssets("t:Pokemon");
        pokemons = new Pokemon[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            pokemons[i] = AssetDatabase.LoadAssetAtPath<Pokemon>(path);
        }

        AssignUniqueIDs();
        EditorUtility.SetDirty(this);
        Debug.Log("Pokemon list refreshed!");
    }
#endif
}
