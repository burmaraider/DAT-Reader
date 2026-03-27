using System.Linq;
using UnityEngine;
using Utility;

public class StartOnGoodComponent : MonoBehaviour
{
    public void Start()
    {
        var worldObjects = FindObjectsByType<WorldObjectComponent>(FindObjectsSortMode.None).ToList();
        var startPoints = worldObjects.Where(x => x.ObjectType == "StartPoint").ToList();

        // Evil TeamNumber = 0, Good TeamNumber = 1
        // To get Good first, OrderByDescending
        var firstPos = startPoints.OrderByDescending(x => x.TeamNumber).ThenBy(x => x.PlayerNumber).FirstOrDefault();

        if (firstPos != null )
        {
            this.transform.position = firstPos.gameObject.transform.position;
            this.transform.rotation = firstPos.gameObject.transform.rotation;
            Debug.Log($"Moving to Team={(firstPos.TeamNumber == 0 ? "Evil" : "Good")}, Pos={firstPos.PlayerNumber}");
        }
        else
        {
            Debug.Log($"Could not move player. No Start Position found");
        }
    }
}
