using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Skripta za objekt lopte.
 * Služi primarno za kontrolu i puštanje zvukova.
 */
public class Ball : MonoBehaviour
{
    private AudioSource[] audioSource;
    private AudioSource bounceSound;
    private AudioSource hoopSound;

    private void Start()
    {
        audioSource = GetComponents<AudioSource>();
        hoopSound = audioSource[0];
        bounceSound = audioSource[1];
    }

    /**
     * Pušta jedan od dva zvuka kada lopta dotakne neku površinu ili obru?.
     */
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("BarPart"))
        {
            bounceSound.Play();
        } else
        {
            hoopSound.Play();
        }
    }
}
