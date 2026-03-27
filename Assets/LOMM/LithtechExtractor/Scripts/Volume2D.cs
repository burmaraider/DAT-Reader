using UnityEngine;

public class Volume2D : MonoBehaviour
{
    public AudioSource audioSource;
    public float minDist = 1;
    public float maxDist = 400;

    private Transform listenerTransform;

    private void Start()
    {
        listenerTransform = Camera.main.transform;
    }

    void Update()
    {
        float dist = Vector3.Distance(transform.position, listenerTransform.position);

        if (dist < minDist)
        {
            audioSource.volume = 1;
        }
        else if (dist > maxDist)
        {
            audioSource.volume = 0;
        }
        else
        {
            audioSource.volume = 1 - ((dist - minDist) / (maxDist - minDist));
        }
    }
}