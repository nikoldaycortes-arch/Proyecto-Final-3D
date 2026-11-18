
using UnityEngine;

public class SonidoHover3D : MonoBehaviour
{
    public AudioClip sonidoHover;
    private AudioSource reproductor;

    private bool mouseEncima = false;

    void Start()
    {
        reproductor = GetComponent<AudioSource>();

        if (reproductor == null)
        {
            reproductor = gameObject.AddComponent<AudioSource>();
        }

        reproductor.playOnAwake = false;
        reproductor.spatialBlend = 0f;
    }

    void OnMouseEnter()
    {
        if (!mouseEncima)
        {
            mouseEncima = true;

            if (sonidoHover != null)
            {
                reproductor.PlayOneShot(sonidoHover);
            }
        }
    }

    void OnMouseExit()
    {
        mouseEncima = false;
    }
}
