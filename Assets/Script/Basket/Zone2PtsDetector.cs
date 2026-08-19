using UnityEngine;

public class Zone2PtsDetector : MonoBehaviour
{
    public int collidersInZone { get; private set; } = 0;
    public TeamName? teamZone { get; private set; } = null;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("2pts"))
        {
            collidersInZone++;

            if (collidersInZone == 1)
            {
                BasketRim rim = other.GetComponentInParent<BasketRim>();
                if (rim == null || rim.RimTeam == null)
                {
                    Debug.LogError($"2-point zone {other.name} has no owning team.");
                    teamZone = null;
                }
                else
                {
                    teamZone = rim.RimTeam.teamName;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("2pts"))
        {
            collidersInZone = Mathf.Max(0, collidersInZone - 1);

            if (collidersInZone == 0)
            {
                teamZone = null;
            }
        }
    }

    public bool IsInOpponent2PtsZone(TeamName selfTeam)
    {
        return teamZone != null && teamZone != selfTeam;
    }
}
