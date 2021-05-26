using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    //External light that will flicker
    public new Light light;
    //Minium random light intesnity
    public float minIntensity = 0f;
    //Maximum random light intesnity
    public float maxIntensity = 1f;
    //How much to smooth out the randomness
    public int smoothing = 5;

    //Continous average calculation via FIFO queue
    //Saves us iterating ever time we update
    Queue<float> smoothQueue;
    float lastSum = 0;

    /// <summary>
    /// Reset the randomness and start again.
    /// </summary>
    public void Reset()
    {
        smoothQueue.Clear();
        lastSum = 0;
    }

    private void Start()
    {
        smoothQueue = new Queue<float>(smoothing);
        if (light == null)
        {
            light = GetComponent<Light>();
        }
    }

    private void Update()
    {
        if (light == null)
            return;

        while(smoothQueue.Count >= smoothing)
        {
            lastSum -= smoothQueue.Dequeue();
        }

        float newVal = Random.Range(minIntensity, maxIntensity);
        smoothQueue.Enqueue(newVal);
        lastSum += newVal;

        light.intensity = lastSum / (float)smoothQueue.Count;
    }
}
