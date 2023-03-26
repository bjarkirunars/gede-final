using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ripple : MonoBehaviour
{

    float _interval;
    float _time;
    public ParticleSystem m_Particles;
    public float bpm = 60f;



    // Start is called before the first frame update
    void Start()
    {
        _time = 0f;

    }

    // Update is called once per frame
    void Update()
    {
        if(bpm >= 50)
        _interval = 60/bpm;
        _time += Time.deltaTime;

        {
            while(_time >= _interval) 
            {
                m_Particles.Play();
                _time -= _interval;
            }
        }
    }
}
