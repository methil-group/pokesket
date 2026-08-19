using System;
using System.Linq;
using TMPro;
using UnityEngine;

public class BasketRim : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI opponentScore;
    [SerializeField] private BasketTeam rimTeam;
    public Cloth netRimCloth;
    public BasketTeam RimTeam => rimTeam;

    private bool hasEnteredFromTop = false;
    private bool hasScored = false;
    // When we hit the balls
    public GameObject particleParent;

    public void OnBallEnter(RimTrigger.TriggerType trigger)
    {
        if (trigger == RimTrigger.TriggerType.First)
        {
            // Entrée par le haut
            hasEnteredFromTop = true;
            hasScored = false;
        }
        else if (trigger == RimTrigger.TriggerType.Last)
        {
            // Si la balle est bien passée par le haut d’abord
            if (hasEnteredFromTop && !hasScored)
            {
                // if(particleParent.)
                Debug.LogWarning("Ball entered top");
                Debug.LogWarning(BasketBallManager.Instance.lastPokemonTypeHolder);
                try
                {
                    var _isntanciedParticles = Instantiate(BasketBallManager.Instance.lastPokemonTypeHolder.particlePointPrefab, particleParent.transform);
                    ParticleSystem[] particleSystems = _isntanciedParticles.GetComponentsInChildren<ParticleSystem>(true);
                    var listParticles = particleSystems.ToList();
                    listParticles.Add(_isntanciedParticles.GetComponent<ParticleSystem>());
                    
                    foreach (var ps in listParticles)
                    {
                        if (ps == null) continue;
                        var renderer = ps.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.sortingOrder = 9000;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Error occured while trying to spawn particle of last pokemon holder");
                    Debug.LogError(e);
                }
                Goal();
                hasScored = true; // Évite les scores multiples
            }
        }
    }

    public void OnBallExit(RimTrigger.TriggerType trigger)
    {
        if (trigger == RimTrigger.TriggerType.First)
        {
            // Si la balle ressort par le haut sans marquer, on annule
            hasEnteredFromTop = false;
            hasScored = false;
        }
    }

    private void Goal()
    {
        BasketBall ball = BasketBallManager.Instance.basketBall;
        if (BasketBallManager.Instance.lastBallHolder != null)
        {
            BasketBallManager.Instance.lastBallHolder.pointScored += ball.points;
        }
        else
        {
            Debug.LogWarning("Cannot set last ball holder ball points");
        }
        
        var opponentTeam =
            GameManager.Instance.GetTeam(rimTeam.teamName == TeamName.Blue ? TeamName.Red : TeamName.Blue);
        opponentTeam.teamScore += ball.points;

        BasketBallManager.Instance.canBeHoldByTeam = rimTeam.teamName;
        
        Debug.Log("PANIER !");
    }
}
