using UnityEngine;

namespace GeeWiz
{
    public class Enemigo : MonoBehaviour, IGolpeable
    {
        public Transform player;

        [Header("Seguimiento")]
        public float rangoPlayer = 7f;
        public float limiteSeguimiento = 10f;
        public float distanciaMinima = 2f;
        public float velocidad = 3f;

        [Header("Patrulla")]
        public float anchoPatrulla = 3f;

        [Header("Combate Medieval")]
        public bool haceDano = false;
        public bool tieneEscudoFrontal = false;
        public float radioContacto = 1.0f;
        public GameObject vfxDerrota;
        public Animator animator;

        [Header("Animacion y Hechizos")]
        public bool esMago = false;
        public float intervaloHechizo = 2.8f;
        public float rangoDisparoMagico = 16f;
        public float velocidadHechizo = 9.5f;

        [Header("Caballero")]
        public bool esCaballero = false;
        public float rangoEspadazo = 2.8f;
        public float pausaEspadazo = 2.2f;

        [Header("Rana Medieval")]
        public bool esRana = false;
        public float alturaSaltoRana = 1.6f;
        public float pausaSaltoRana = 0.65f;

        private Vector3 puntoInicio;
        private Vector3 puntoIzquierdo;
        private Vector3 puntoDerecho;
        private Vector3 destinoPatrulla;
        private bool muerto = false;
        private string paramVelocidad;
        private Transform visualHijo;
        private Vector3 posicionVisualBase;
        private Vector3 escalaVisualBase = Vector3.one;
        private float proximoHechizo = 0f;
        private float ultimoHechizo = -10f;
        private float proximoEspadazo = 0f;
        private bool ranaEnSalto = false;
        private float tiempoFaseRana = 0f;
        private Vector3 inicioSaltoRana;
        private Vector3 finSaltoRana;
        private float baseYRana;
        private const float DuracionSaltoRana = 0.55f;

        private void Start()
        {
            puntoInicio = transform.position;
            baseYRana = puntoInicio.y;
            //Patrulla de lado a lado del camino aunque este curvo
            puntoIzquierdo = puntoInicio - transform.right * anchoPatrulla;
            puntoDerecho = puntoInicio + transform.right * anchoPatrulla;
            destinoPatrulla = puntoDerecho;

            if (transform.childCount > 0)
            {
                visualHijo = transform.GetChild(0);
                posicionVisualBase = visualHijo.localPosition;
                escalaVisualBase = visualHijo.localScale;
            }
            proximoHechizo = Time.time + Random.Range(0.8f, intervaloHechizo);

            if (player == null)
            {
                GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
                if (objJugador != null) player = objJugador.transform;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter p in animator.parameters)
                {
                    if (p.type == AnimatorControllerParameterType.Float && (p.name == "Velocidad" || p.name == "Speed"))
                    {
                        paramVelocidad = p.name;
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (muerto) return;
            if (GestorJuego.Instancia != null && (GestorJuego.Instancia.juegoPausado || GestorJuego.Instancia.juegoTerminado))
            {
                return;
            }

            float velocidadMovimiento = 0f;

            if (esRana && !esMago)
            {
                velocidadMovimiento = ActualizarRana();
            }
            else if (player != null)
            {
                Vector3 destino = player.position;
                //Persigue solo en el plano, sin subir ni bajar
                destino.y = transform.position.y;

                float distancia = Vector3.Distance(transform.position, destino);
                float lejosDelInicio = Vector3.Distance(puntoInicio, destino);

                if (distancia <= rangoPlayer && lejosDelInicio <= limiteSeguimiento)
                {
                    RotarHacia(destino);
                    if (distancia > distanciaMinima)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
                        velocidadMovimiento = velocidad;
                    }
                }
                else if (esMago && distancia <= rangoDisparoMagico)
                {
                    RotarHacia(destino);
                }
                else
                {
                    Patrullar();
                    velocidadMovimiento = velocidad;
                }

                if (esMago && distancia <= rangoDisparoMagico && Time.time >= proximoHechizo)
                {
                    LanzarHechizo(player.position + Vector3.up * 0.9f);
                    proximoHechizo = Time.time + intervaloHechizo;
                }
            }
            else
            {
                Patrullar();
                velocidadMovimiento = velocidad;
            }

            if (visualHijo != null)
            {
                if (esMago)
                {
                    visualHijo.localPosition = posicionVisualBase + Vector3.up * (Mathf.Sin(Time.time * 3.2f) * 0.1f);
                    //Se echa hacia atras y luego lanza el hechizo hacia adelante
                    float fase = Mathf.Clamp01((Time.time - ultimoHechizo) / 0.6f);
                    float inclinacion = fase < 1f ? Mathf.Sin(fase * Mathf.PI * 2f) * -18f : 0f;
                    visualHijo.localRotation = Quaternion.Euler(inclinacion, 0f, Mathf.Sin(Time.time * 2f) * 4f);
                }
                else if (tieneEscudoFrontal)
                {
                    float paso = velocidadMovimiento > 0.05f ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.05f : 0f;
                    visualHijo.localPosition = posicionVisualBase + Vector3.up * paso;
                }
                visualHijo.localScale = Vector3.Lerp(visualHijo.localScale, escalaVisualBase, 10f * Time.deltaTime);
            }

            if (animator != null && !string.IsNullOrEmpty(paramVelocidad))
            {
                animator.SetFloat(paramVelocidad, velocidadMovimiento);
            }

            if (esCaballero && animator != null && player != null && Time.time >= proximoEspadazo)
            {
                Vector3 jugadorPlano = new Vector3(player.position.x, transform.position.y, player.position.z);
                if (Vector3.Distance(transform.position, jugadorPlano) <= rangoEspadazo)
                {
                    animator.SetTrigger("Atacar");
                    proximoEspadazo = Time.time + pausaEspadazo;
                }
            }

            VerificarContactoLateral();
        }

        private void LanzarHechizo(Vector3 objetivo)
        {
            ultimoHechizo = Time.time;
            Vector3 origen = transform.position + transform.forward * 0.8f + Vector3.up * 1.1f;
            Vector3 direccion = (objetivo - origen).normalized;

            GameObject orbe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbe.name = "HechizoEnemigo";
            orbe.transform.position = origen;
            orbe.transform.localScale = Vector3.one * 0.42f;

            SphereCollider col = orbe.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;

            Renderer rend = orbe.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat = shader != null ? new Material(shader) : new Material(rend.sharedMaterial);
                Color colorOrbe = new Color(0.65f, 0.2f, 1f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colorOrbe);
                mat.color = colorOrbe;
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", new Color(0.2f, 0.85f, 1f) * 2.2f);
                rend.material = mat;
            }

            ProyectilWumpa proyectil = orbe.AddComponent<ProyectilWumpa>();
            proyectil.tiempoVida = 3.2f;
            proyectil.InicializarHechizo(direccion, velocidadHechizo, gameObject);

            if (visualHijo != null)
            {
                visualHijo.localScale = escalaVisualBase * 1.22f;
            }
            GestorJuego.Instancia?.ReproducirSFX("wumpa");
        }

        public void NotificarBloqueoEscudo()
        {
            if (visualHijo != null)
            {
                visualHijo.localScale = Vector3.Scale(escalaVisualBase, new Vector3(1.12f, 0.92f, 1.12f));
            }
        }

        private float ActualizarRana()
        {
            if (!ranaEnSalto)
            {
                tiempoFaseRana += Time.deltaTime;
                Vector3 objetivoMirar = destinoPatrulla;

                if (player != null)
                {
                    Vector3 posJugadorPlano = new Vector3(player.position.x, baseYRana, player.position.z);
                    float distJugador = Vector3.Distance(new Vector3(transform.position.x, baseYRana, transform.position.z), posJugadorPlano);
                    float lejosInicio = Vector3.Distance(puntoInicio, posJugadorPlano);
                    if (distJugador <= rangoPlayer && lejosInicio <= limiteSeguimiento)
                    {
                        objetivoMirar = posJugadorPlano;
                    }
                }

                RotarHacia(objetivoMirar);
                if (tiempoFaseRana < pausaSaltoRana) return 0f;

                Vector3 origenPlano = new Vector3(transform.position.x, baseYRana, transform.position.z);
                Vector3 delta = objetivoMirar - origenPlano;
                delta.y = 0f;
                float distancia = delta.magnitude;
                float paso = Mathf.Min(distancia, Mathf.Max(2.0f, velocidad * 0.85f));
                inicioSaltoRana = origenPlano;
                finSaltoRana = delta.sqrMagnitude > 0.001f ? origenPlano + delta.normalized * paso : origenPlano;

                if (objetivoMirar == destinoPatrulla && Vector3.Distance(finSaltoRana, destinoPatrulla) < 0.25f)
                {
                    destinoPatrulla = destinoPatrulla == puntoDerecho ? puntoIzquierdo : puntoDerecho;
                }

                ranaEnSalto = true;
                tiempoFaseRana = 0f;
                if (visualHijo != null)
                {
                    visualHijo.localScale = Vector3.Scale(escalaVisualBase, new Vector3(0.84f, 1.24f, 0.84f));
                }
                return velocidad;
            }

            tiempoFaseRana += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoFaseRana / DuracionSaltoRana);
            Vector3 pos = Vector3.Lerp(inicioSaltoRana, finSaltoRana, t);
            pos.y = baseYRana + 4f * alturaSaltoRana * t * (1f - t);
            transform.position = pos;
            RotarHacia(finSaltoRana);

            if (t >= 1f)
            {
                transform.position = finSaltoRana;
                ranaEnSalto = false;
                tiempoFaseRana = 0f;
                if (visualHijo != null)
                {
                    visualHijo.localScale = Vector3.Scale(escalaVisualBase, new Vector3(1.24f, 0.74f, 1.24f));
                }
            }

            return velocidad;
        }

        private void Patrullar()
        {
            RotarHacia(destinoPatrulla);
            transform.position = Vector3.MoveTowards(transform.position, destinoPatrulla, velocidad * Time.deltaTime);

            if (Vector3.Distance(transform.position, destinoPatrulla) < 0.1f)
            {
                destinoPatrulla = destinoPatrulla == puntoDerecho ? puntoIzquierdo : puntoDerecho;
            }
        }

        private void RotarHacia(Vector3 objetivo)
        {
            Vector3 dir = objetivo - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion rotObjetivo = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotObjetivo, 8f * Time.deltaTime);
        }

        private void VerificarContactoLateral()
        {
            if (!haceDano || muerto || player == null) return;

            float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(player.position.x, player.position.z));
            if (distXZ >= radioContacto) return;

            //Si le cae encima cuenta como salto y no hace dano
            if (player.position.y >= transform.position.y + 0.9f || player.position.y < transform.position.y - 1.5f) return;

            AtaqueGiro giro = player.GetComponent<AtaqueGiro>();
            if (giro != null && giro.EstaGirando && (!tieneEscudoFrontal || PuedeRecibirGiroDesde(player.position))) return;

            player.GetComponent<MovoJugadorTPV>()?.RecibirDano();
        }

        public bool PuedeRecibirGiroDesde(Vector3 posicionAtacante)
        {
            if (!tieneEscudoFrontal) return true;

            Vector3 dirHaciaAtacante = posicionAtacante - transform.position;
            dirHaciaAtacante.y = 0f;
            if (dirHaciaAtacante.sqrMagnitude < 0.001f) return true;

            return Vector3.Dot(transform.forward, dirHaciaAtacante.normalized) <= 0.25f;
        }

        public void Golpear()
        {
            if (muerto) return;
            muerto = true;

            if (vfxDerrota != null)
            {
                Instantiate(vfxDerrota, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            }

            GestorJuego.Instancia?.ReproducirSFX("hit");
            Debug.Log("Enemigo eliminado: " + name);
            Destroy(gameObject);
        }
    }
}
