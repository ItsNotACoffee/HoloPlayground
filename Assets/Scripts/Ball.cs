using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Skripta za objekt lopte.
 * Slu�i primarno za kontrolu i pu�tanje zvukova.
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
     * Pu�ta jedan od dva zvuka kada lopta dotakne neku povr�inu ili obru?.
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
