using UnityEngine;
using System.Collections;

public class GameStartPointEditor : MonoBehaviour
{

    public Material mat;
    void OnDrawGizmos()
    {
        // Draws the Light bulb icon at position of the object.
        // Because we draw it inside OnDrawGizmos the icon is also pickable
        // in the scene view.

        Gizmos.DrawIcon(transform.position, "GameStartPoint.png", true);
    }
}