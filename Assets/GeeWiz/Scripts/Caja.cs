using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeeWiz
{
    public class Caja : MonoBehaviour, IGolpeable
    {
        public enum TipoCaja { MaderaWumpa, Interrogacion, Checkpoint, TNT, Nitro, MetalicaRebote, Tiempo, InterruptorNitro, InterruptorMetal, AkuAku }

        public TipoCaja tipo = TipoCaja.MaderaWumpa;
        public int wumpasQueSuelta = 3;
        public float segundosTiempo = 2f;
        public float radioExplosion = 3.5f;
        public float tiempoTNT = 3f;
        public GameObject vfxRuptura;
        public GameObject vfxExplosion;
        public GameObject prefabWumpa;
        public bool esContornoFantasma = false;
        public int grupoFantasma = 1;

        public bool YaActivada => yaActivada;

        private bool rota = false;
        private bool yaExplotada = false;
        private bool yaActivada = false;
        private bool cuentaTNTActiva = false;
        private Transform visualTransform;
        private Vector3 posVisualBase;
        private Vector3 escalaVisualBase = Vector3.one;
        private Vector3 escalaBasePropia = Vector3.one;
        private float faseSaltoNitro;
        private Coroutine rutinaRebote;
        private BoxCollider colliderSolido;
        private Renderer[] renderersVisual;
        private Material[][] materialesOriginales;

        private void Start()
        {
            escalaBasePropia = transform.localScale;
            visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                posVisualBase = visualTransform.localPosition;
                escalaVisualBase = visualTransform.localScale;
            }
            faseSaltoNitro = Random.Range(0f, 1.4f);

            foreach (BoxCollider bc in GetComponents<BoxCollider>())
            {
                if (!bc.isTrigger)
                {
                    colliderSolido = bc;
                    break;
                }
            }
            if (colliderSolido == null)
            {
                colliderSolido = GetComponentInChildren<BoxCollider>();
            }

            renderersVisual = visualTransform != null
                ? visualTransform.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);
            materialesOriginales = new Material[renderersVisual.Length][];
            for (int i = 0; i < renderersVisual.Length; i++)
            {
                materialesOriginales[i] = renderersVisual[i].sharedMaterials;
            }

            if (esContornoFantasma)
            {
                if (colliderSolido != null) colliderSolido.enabled = false;
                AplicarMaterialFantasma();
            }

            if (tipo != TipoCaja.MetalicaRebote && tipo != TipoCaja.InterruptorNitro && tipo != TipoCaja.InterruptorMetal && GestorJuego.Instancia != null)
            {
                GestorJuego.Instancia.RegistrarCaja(this);
            }
        }

        private void AplicarMaterialFantasma()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material matFantasma = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            Color blancoFantasma = new Color(1f, 1f, 1f, 0.32f);

            if (matFantasma.HasProperty("_Surface")) matFantasma.SetFloat("_Surface", 1f);
            if (matFantasma.HasProperty("_Blend")) matFantasma.SetFloat("_Blend", 0f);
            if (matFantasma.HasProperty("_BaseColor")) matFantasma.SetColor("_BaseColor", blancoFantasma);
            matFantasma.color = blancoFantasma;
            //Material transparente para cuando la caja es fantasma
            matFantasma.SetOverrideTag("RenderType", "Transparent");
            matFantasma.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            matFantasma.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            matFantasma.SetInt("_ZWrite", 0);
            matFantasma.DisableKeyword("_ALPHATEST_ON");
            matFantasma.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            matFantasma.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            for (int i = 0; i < renderersVisual.Length; i++)
            {
                int cantidad = Mathf.Max(1, materialesOriginales[i].Length);
                Material[] mats = new Material[cantidad];
                for (int m = 0; m < cantidad; m++) mats[m] = matFantasma;
                renderersVisual[i].materials = mats;
            }
        }

        public void MaterializarFantasma()
        {
            if (!esContornoFantasma) return;
            esContornoFantasma = false;

            if (renderersVisual != null && materialesOriginales != null)
            {
                for (int i = 0; i < renderersVisual.Length; i++)
                {
                    if (renderersVisual[i] != null && materialesOriginales[i] != null)
                    {
                        renderersVisual[i].sharedMaterials = materialesOriginales[i];
                    }
                }
            }

            if (colliderSolido != null) colliderSolido.enabled = true;
            AnimarRebote();
        }

        private void AplicarColorInactivo()
        {
            Color grisApagado = new Color(0.42f, 0.44f, 0.47f, 1f);
            if (renderersVisual != null)
            {
                foreach (Renderer rend in renderersVisual)
                {
                    if (rend == null) continue;
                    foreach (Material mat in rend.materials)
                    {
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", grisApagado);
                        mat.color = grisApagado;
                    }
                }
            }

            if (visualTransform != null)
            {
                posVisualBase += Vector3.down * 0.08f;
                visualTransform.localPosition = posVisualBase;
            }
        }

        private void Update()
        {
            if (tipo == TipoCaja.Nitro && !yaExplotada && visualTransform != null)
            {
                //Movemos solo el modelo visual para no afectar el collider
                float t = (Time.time + faseSaltoNitro) % 1.4f;
                if (t < 0.24f)
                {
                    float p = t / 0.24f;
                    visualTransform.localPosition = posVisualBase + Vector3.up * (Mathf.Sin(p * Mathf.PI) * 0.16f);
                    visualTransform.localScale = Vector3.Scale(escalaVisualBase, new Vector3(0.94f, 1.08f, 0.94f));
                }
                else if (t < 0.38f)
                {
                    float s = 1f - ((t - 0.24f) / 0.14f);
                    visualTransform.localPosition = posVisualBase;
                    visualTransform.localScale = Vector3.Lerp(escalaVisualBase, Vector3.Scale(escalaVisualBase, new Vector3(1.12f, 0.86f, 1.12f)), s);
                }
                else
                {
                    visualTransform.localPosition = posVisualBase;
                    visualTransform.localScale = escalaVisualBase;
                }
            }
        }

        public void AnimarRebote()
        {
            if (rota || yaExplotada) return;
            if (rutinaRebote != null)
            {
                StopCoroutine(rutinaRebote);
            }
            rutinaRebote = StartCoroutine(RutinaRebote());
        }

        private IEnumerator RutinaRebote()
        {
            Transform objetivo = visualTransform != null ? visualTransform : transform;
            Vector3 escalaOriginal = visualTransform != null ? escalaVisualBase : escalaBasePropia;
            Vector3 escalaAplastada = Vector3.Scale(escalaOriginal, new Vector3(1.22f, 0.76f, 1.22f));
            Vector3 escalaEstirada = Vector3.Scale(escalaOriginal, new Vector3(0.9f, 1.12f, 0.9f));

            float duracion = 0.2f;
            float t = 0f;
            while (t < duracion && objetivo != null)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duracion);
                if (p < 0.35f)
                {
                    objetivo.localScale = Vector3.Lerp(escalaOriginal, escalaAplastada, p / 0.35f);
                }
                else if (p < 0.75f)
                {
                    objetivo.localScale = Vector3.Lerp(escalaAplastada, escalaEstirada, (p - 0.35f) / 0.4f);
                }
                else
                {
                    objetivo.localScale = Vector3.Lerp(escalaEstirada, escalaOriginal, (p - 0.75f) / 0.25f);
                }
                yield return null;
            }

            if (objetivo != null)
            {
                objetivo.localScale = escalaOriginal;
            }
            rutinaRebote = null;
        }

        public void ActivarCuentaRegresivaTNT()
        {
            if (cuentaTNTActiva || yaExplotada || rota) return;
            cuentaTNTActiva = true;
            StartCoroutine(CuentaRegresivaTNT());
        }

        private IEnumerator CuentaRegresivaTNT()
        {
            Vector3 escalaBase = transform.localScale;
            Renderer rend = GetComponentInChildren<Renderer>();
            bool tieneColorBase = rend != null && (rend.material.HasProperty("_BaseColor") || rend.material.HasProperty("_Color"));
            Color colorOriginal = tieneColorBase ? rend.material.color : Color.white;
            float transcurrido = 0f;
            int ultimoSegundo = -1;

            while (transcurrido < tiempoTNT && !yaExplotada)
            {
                transcurrido += Time.deltaTime;
                int segundoActual = Mathf.FloorToInt(transcurrido);
                if (segundoActual != ultimoSegundo)
                {
                    ultimoSegundo = segundoActual;
                    GestorJuego.Instancia?.ReproducirSFX("tick");
                }

                float pulso = 1f + Mathf.Abs(Mathf.Sin(transcurrido * 8f)) * 0.14f;
                transform.localScale = escalaBase * pulso;

                if (tieneColorBase)
                {
                    bool parpadeo = Mathf.PingPong(transcurrido * 10f, 1f) > 0.5f;
                    rend.material.color = parpadeo ? Color.red : colorOriginal;
                }

                yield return null;
            }

            Explotar();
        }

        public void Explotar()
        {
            Explotar(true);
        }

        public void ExplotarSinDanoJugador()
        {
            Explotar(false);
        }

        public void Explotar(bool danarJugador)
        {
            if (yaExplotada) return;
            yaExplotada = true;
            rota = true;

            if (vfxExplosion != null)
            {
                Instantiate(vfxExplosion, transform.position, Quaternion.identity);
            }

            GestorJuego.Instancia?.ReproducirSFX("explosion");

            Collider[] cercanos = Physics.OverlapSphere(transform.position, radioExplosion);
            HashSet<GameObject> afectados = new HashSet<GameObject>();

            foreach (Collider col in cercanos)
            {
                GameObject raiz = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
                if (afectados.Contains(raiz)) continue;
                afectados.Add(raiz);

                MovoJugadorTPV jugador = col.GetComponent<MovoJugadorTPV>() ?? col.GetComponentInParent<MovoJugadorTPV>();
                if (jugador != null)
                {
                    if (danarJugador) jugador.RecibirDano();
                    continue;
                }

                Enemigo enemigo = col.GetComponent<Enemigo>() ?? col.GetComponentInParent<Enemigo>();
                if (enemigo != null)
                {
                    enemigo.Golpear();
                    continue;
                }

                Caja otraCaja = col.GetComponent<Caja>() ?? col.GetComponentInParent<Caja>();
                if (otraCaja != null && otraCaja != this)
                {
                    if (otraCaja.esContornoFantasma) continue;
                    if (otraCaja.tipo == TipoCaja.TNT || otraCaja.tipo == TipoCaja.Nitro)
                    {
                        otraCaja.Explotar(danarJugador);
                    }
                    else
                    {
                        otraCaja.Golpear();
                    }
                    continue;
                }

                IGolpeable golpeable = col.GetComponent<IGolpeable>() ?? col.GetComponentInParent<IGolpeable>();
                if (golpeable != null && !(golpeable is Caja))
                {
                    golpeable.Golpear();
                }
            }

            GestorJuego.Instancia?.NotificarCajaRota(this);
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (tipo == TipoCaja.Nitro && !yaExplotada && !esContornoFantasma && other.CompareTag("Player"))
            {
                Explotar();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (tipo == TipoCaja.Nitro && !yaExplotada && !esContornoFantasma && collision.gameObject.CompareTag("Player"))
            {
                Explotar();
            }
        }

        public void Golpear()
        {
            if (rota || esContornoFantasma) return;

            if (tipo == TipoCaja.MetalicaRebote)
            {
                GestorJuego.Instancia?.ReproducirSFX("crate");
                return;
            }

            if (tipo == TipoCaja.InterruptorNitro)
            {
                if (!yaActivada)
                {
                    yaActivada = true;
                    AplicarColorInactivo();
                    AnimarRebote();
                    GestorJuego.Instancia?.ReproducirSFX("checkpoint");
                    GestorJuego.Instancia?.DetonarTodasLasNitros();
                }
                else
                {
                    GestorJuego.Instancia?.ReproducirSFX("crate");
                }
                return;
            }

            if (tipo == TipoCaja.InterruptorMetal)
            {
                if (!yaActivada)
                {
                    yaActivada = true;
                    AplicarColorInactivo();
                    AnimarRebote();
                    GestorJuego.Instancia?.ReproducirSFX("checkpoint");
                    GestorJuego.Instancia?.ActivarCajasFantasma(grupoFantasma);
                }
                else
                {
                    GestorJuego.Instancia?.ReproducirSFX("crate");
                }
                return;
            }

            if (tipo == TipoCaja.TNT || tipo == TipoCaja.Nitro)
            {
                Explotar();
                return;
            }

            //La de interrogacion es una apuesta: la mitad de las veces da la mascara y la otra mitad explota
            if (tipo == TipoCaja.Interrogacion)
            {
                if (Random.value < 0.5f)
                {
                    tipo = TipoCaja.AkuAku;
                }
                else
                {
                    Explotar();
                    return;
                }
            }

            rota = true;

            if (tipo == TipoCaja.Checkpoint)
            {
                MovoJugadorTPV jugador = FindAnyObjectByType<MovoJugadorTPV>();
                if (jugador != null)
                {
                    jugador.ActualizarCheckpoint(transform.position);
                }
                GestorJuego.Instancia?.ReproducirSFX("checkpoint");
            }
            else if (tipo == TipoCaja.Tiempo)
            {
                GestorJuego.Instancia?.CongelarTiempo(segundosTiempo);
            }
            else if (tipo == TipoCaja.AkuAku)
            {
                MovoJugadorTPV jugador = FindAnyObjectByType<MovoJugadorTPV>();
                jugador?.OtorgarAkuAku();
                GestorJuego.Instancia?.ReproducirSFX("checkpoint");
            }
            else
            {
                if (prefabWumpa != null)
                {
                    for (int i = 0; i < wumpasQueSuelta; i++)
                    {
                        GameObject fruta = Instantiate(prefabWumpa, transform.position + Vector3.up * 0.4f, Quaternion.identity);
                        FrutaWumpa wumpa = fruta.GetComponent<FrutaWumpa>();
                        if (wumpa != null)
                        {
                            Vector3 salida = new Vector3(Random.Range(-2f, 2f), Random.Range(4.5f, 6.5f), Random.Range(-2f, 2f));
                            wumpa.Expulsar(salida, 0.45f);
                        }
                    }
                }
                else
                {
                    GestorJuego.Instancia?.SumarWumpa(wumpasQueSuelta);
                }
                GestorJuego.Instancia?.ReproducirSFX("crate");
            }

            if (vfxRuptura != null)
            {
                Instantiate(vfxRuptura, transform.position, Quaternion.identity);
            }

            GestorJuego.Instancia?.NotificarCajaRota(this);
            Destroy(gameObject);
        }
    }
}
