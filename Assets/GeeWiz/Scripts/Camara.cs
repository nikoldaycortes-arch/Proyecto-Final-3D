using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeeWiz
{
    public class Camara : MonoBehaviour
    {
        //Rotaciones en X y Y
        public float rotX = 25f;
        public float rotY = 0f;

        //Nuestro objeto a seguir
        public Transform target;

        //Distancia de la camara a nuestro objeto a seguir
        public float distanceTarget = 3.0f;
        public Vector3 offsetMira = Vector3.zero;
        public float suavizado = 0f;

        //Puntos del camino curvo, la camara gira siguiendo el rumbo del mas cercano
        public Transform rutaCamara;
        public float suavizadoRuta = 3f;
        //Toma el rumbo de un punto que queda atras de Crash para que la curva se vea venir antes de girar
        public int puntosRetraso = 2;
        private float rumboRuta = 0f;

        private float rotXBase;
        private float rotYBase;
        private float distBase;
        private float distanciaActual;
        private bool vistaBonus = false;
        private bool enTransicionVista = false;
        private Vector3 velSuavizado = Vector3.zero;

        void Start()
        {
            //Bloqueamos la posicion del mouse y lo desaparecemos de la pantalla
            Cursor.lockState = CursorLockMode.Locked;
            rotXBase = rotX;
            rotYBase = rotY;
            distBase = distanceTarget;
            distanciaActual = distanceTarget;
        }

        public void ConfigurarVistaBonus(bool activa)
        {
            vistaBonus = activa;
            enTransicionVista = true;

            if (suavizado <= 0.001f)
            {
                rotX = activa ? 10f : rotXBase;
                rotY = activa ? -78f : rotYBase;
                distanceTarget = activa ? 10.5f : distBase;
                distanciaActual = distanceTarget;
                enTransicionVista = false;
            }
        }

        public void OrientarHaciaAdelante()
        {
            vistaBonus = false;
            enTransicionVista = false;
            rotX = 16f;
            rotY = 0f;
            if (distBase > 0f)
            {
                distanceTarget = distBase;
                distanciaActual = distBase;
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (enTransicionVista)
            {
                float objRotX = vistaBonus ? 10f : rotXBase;
                float objRotY = vistaBonus ? -78f : rotYBase;
                float objDist = vistaBonus ? 10.5f : distBase;
                float t = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.05f, suavizado));

                rotX = Mathf.Lerp(rotX, objRotX, t);
                rotY = Mathf.Lerp(rotY, objRotY, t);
                distanciaActual = Mathf.Lerp(distanciaActual, objDist, t);
                distanceTarget = distanciaActual;

                if (!vistaBonus &&
                    Mathf.Abs(rotX - objRotX) < 0.05f &&
                    Mathf.Abs(rotY - objRotY) < 0.05f &&
                    Mathf.Abs(distanciaActual - objDist) < 0.05f)
                {
                    rotX = objRotX;
                    rotY = objRotY;
                    distanciaActual = objDist;
                    distanceTarget = objDist;
                    enTransicionVista = false;
                }
            }
            else
            {
                distanciaActual = distanceTarget;
            }

            ActualizarRumboRuta();
            HandleRotation();
            HandlePosition();
        }

        void ActualizarRumboRuta()
        {
            if (rutaCamara == null || rutaCamara.childCount == 0) return;

            int indiceCercano = 0;
            float mejor = float.MaxValue;
            for (int i = 0; i < rutaCamara.childCount; i++)
            {
                float distancia = (rutaCamara.GetChild(i).position - target.position).sqrMagnitude;
                if (distancia < mejor)
                {
                    mejor = distancia;
                    indiceCercano = i;
                }
            }
            Transform cercano = rutaCamara.GetChild(Mathf.Max(0, indiceCercano - puntosRetraso));
            rumboRuta = Mathf.LerpAngle(rumboRuta, cercano.eulerAngles.y, suavizadoRuta * Time.deltaTime);
        }

        void HandleRotation()
        {
            transform.rotation = Quaternion.Euler(rotX, rotY + rumboRuta, 0);
        }

        void HandlePosition()
        {
            Vector3 puntoMira = target.position + offsetMira;
            Vector3 posDeseada = puntoMira - transform.forward * distanciaActual;
            if (suavizado > 0.001f)
            {
                transform.position = Vector3.SmoothDamp(transform.position, posDeseada, ref velSuavizado, suavizado);
            }
            else
            {
                transform.position = posDeseada;
            }
        }
    }
}
