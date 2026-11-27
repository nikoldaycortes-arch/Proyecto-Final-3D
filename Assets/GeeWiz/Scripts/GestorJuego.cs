using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeeWiz
{
    public class GestorJuego : MonoBehaviour
    {
        public static GestorJuego Instancia { get; private set; }

        [Header("Estado de Partida")]
        public int vidas = 3;
        public int wumpas = 0;
        public int cajasTotales = 0;
        public int cajasRotas = 0;
        public bool gemaObtenida = false;
        public bool cristalObtenido = false;
        public string reliquiaObtenida = "Ninguna";
        public int escudosAku = 0;
        public GameObject objetoGemaMeta;
        public bool timeTrialActivo = false;
        public float tiempoTranscurrido = 0f;
        public float tiempoCongeladoRestante = 0f;
        public bool juegoPausado = false;
        public bool juegoTerminado = false;

        [Header("UI Opcional")]
        public Canvas canvasHud;

        private readonly HashSet<Caja> cajasRegistradas = new HashSet<Caja>();
        private readonly HashSet<Caja> cajasRotasSet = new HashSet<Caja>();
        private readonly Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();

        private AudioSource musicaSource;
        private AudioSource sfxSource;
        private bool relojDetenidoEnMeta = false;

        private Text txtVidas;
        private Text txtAkuAku;
        private Text txtWumpas;
        private Text txtCajas;
        private Text txtGema;
        private Text txtTiempo;
        private GameObject panelPausa;
        private GameObject panelGameOver;
        private GameObject panelVictoria;
        private Text txtResumenVictoria;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;
            Time.timeScale = 1f;

            InicializarAudio();
            InicializarUI();
            ActualizarHUD();
        }

        private void Update()
        {
            if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) && !juegoTerminado)
            {
                AlternarPausa();
            }

            if (timeTrialActivo && !relojDetenidoEnMeta && !juegoPausado && !juegoTerminado)
            {
                if (tiempoCongeladoRestante > 0f)
                {
                    tiempoCongeladoRestante = Mathf.Max(0f, tiempoCongeladoRestante - Time.deltaTime);
                }
                else
                {
                    tiempoTranscurrido += Time.deltaTime;
                }
            }

            ActualizarHUD();
        }

        public void RegistrarCaja(Caja caja)
        {
            if (caja == null || cajasRegistradas.Contains(caja)) return;
            cajasRegistradas.Add(caja);
            cajasTotales = cajasRegistradas.Count;
            ActualizarHUD();
        }

        public void NotificarCajaRota(Caja caja)
        {
            if (caja == null || cajasRotasSet.Contains(caja)) return;
            cajasRotasSet.Add(caja);
            cajasRotas = cajasRotasSet.Count;

            if (cajasTotales > 0 && cajasRotas >= cajasTotales)
            {
                if (objetoGemaMeta != null) objetoGemaMeta.SetActive(true);
                if (!gemaObtenida) ObtenerGema();
            }

            ActualizarHUD();
        }

        public void SumarWumpa(int cantidad)
        {
            if (cantidad <= 0 || juegoTerminado) return;
            wumpas += cantidad;

            bool vidaExtra = false;
            while (wumpas >= 100)
            {
                wumpas -= 100;
                vidas++;
                vidaExtra = true;
            }

            ReproducirSFX(vidaExtra ? "checkpoint" : "wumpa");
            ActualizarHUD();
        }

        public void ObtenerGema()
        {
            if (gemaObtenida) return;
            gemaObtenida = true;
            ReproducirSFX("victory");
            ActualizarHUD();
        }

        public void ObtenerCristal()
        {
            if (cristalObtenido) return;
            cristalObtenido = true;
            ReproducirSFX("victory");
            ActualizarHUD();
        }

        public void ActualizarAkuAku(int nivel)
        {
            escudosAku = Mathf.Clamp(nivel, 0, 3);
            ActualizarHUD();
        }

        public void DetonarTodasLasNitros()
        {
            StartCoroutine(RutinaDetonarNitros());
        }

        private IEnumerator RutinaDetonarNitros()
        {
            List<Caja> snapshot = new List<Caja>(cajasRegistradas);
            foreach (Caja caja in snapshot)
            {
                if (caja != null && caja.tipo == Caja.TipoCaja.Nitro)
                {
                    caja.ExplotarSinDanoJugador();
                    yield return new WaitForSeconds(0.06f);
                }
            }
        }

        public void ActivarCajasFantasma(int grupo)
        {
            Caja[] todas = FindObjectsByType<Caja>();
            foreach (Caja c in todas)
            {
                if (c != null && c.esContornoFantasma && c.grupoFantasma == grupo)
                {
                    c.MaterializarFantasma();
                }
            }
        }

        public void RestarVida()
        {
            if (juegoTerminado) return;
            vidas = Mathf.Max(0, vidas - 1);
            ReproducirSFX("hit");

            if (vidas <= 0)
            {
                juegoTerminado = true;
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (panelGameOver != null) panelGameOver.SetActive(true);
            }

            ActualizarHUD();
        }

        public void IniciarTimeTrial()
        {
            if (timeTrialActivo) return;
            timeTrialActivo = true;
            relojDetenidoEnMeta = false;
            tiempoTranscurrido = 0f;
            tiempoCongeladoRestante = 0f;
            ReproducirSFX("tick");
            ActualizarHUD();
        }

        public void DetenerRelojMeta()
        {
            relojDetenidoEnMeta = true;
        }

        public string EvaluarReliquia()
        {
            if (!timeTrialActivo) return "Ninguna";
            if (tiempoTranscurrido <= 45f) return "Platino";
            if (tiempoTranscurrido <= 65f) return "Oro";
            return "Zafiro";
        }

        public void CongelarTiempo(float seg)
        {
            if (seg <= 0f) return;
            tiempoCongeladoRestante += seg;
            ReproducirSFX("tick");
            ActualizarHUD();
        }

        public void CompletarNivel()
        {
            if (juegoTerminado) return;

            //Si rompe todas las cajas activamos la gema
            if (cajasTotales > 0 && cajasRotas >= cajasTotales)
            {
                gemaObtenida = true;
            }

            if (timeTrialActivo)
            {
                reliquiaObtenida = EvaluarReliquia();
                float mejorPrevio = PlayerPrefs.GetFloat("GeeWiz_BestTime_1", float.MaxValue);
                if (tiempoTranscurrido < mejorPrevio)
                {
                    PlayerPrefs.SetFloat("GeeWiz_BestTime_1", tiempoTranscurrido);
                    PlayerPrefs.Save();
                }
            }
            else
            {
                reliquiaObtenida = "Ninguna";
            }

            juegoTerminado = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (txtResumenVictoria != null)
            {
                string estadoCristal = cristalObtenido ? "SI" : "NO";
                string estadoGema = gemaObtenida ? "SI" : "NO";
                string tiempoTexto = timeTrialActivo ? FormatearTiempo(tiempoTranscurrido) : "Sin Time Trial";
                txtResumenVictoria.text =
                    $"Cajas: {cajasRotas} / {cajasTotales} | Wumpas: {wumpas}\n" +
                    $"Cristal: {estadoCristal} | Gema: {estadoGema}\n" +
                    $"Reliquia: {reliquiaObtenida} | Tiempo: {tiempoTexto}";
            }

            if (panelVictoria != null) panelVictoria.SetActive(true);
            ReproducirSFX("victory");
            ActualizarHUD();
        }

        public void AlternarPausa()
        {
            if (juegoTerminado) return;

            juegoPausado = !juegoPausado;
            Time.timeScale = juegoPausado ? 0f : 1f;
            Cursor.lockState = juegoPausado ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = juegoPausado;

            if (panelPausa != null) panelPausa.SetActive(juegoPausado);
        }

        public void ReiniciarNivel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void IrAlMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            int actual = SceneManager.GetActiveScene().buildIndex;
            int destino = (SceneManager.sceneCountInBuildSettings > 1 && actual != 0) ? 0 : actual;
            SceneManager.LoadScene(destino);
        }

        public void ReproducirSFX(string nombre)
        {
            if (sfxSource == null || string.IsNullOrEmpty(nombre)) return;
            if (sfxClips.TryGetValue(nombre, out AudioClip clip) && clip != null)
            {
                sfxSource.PlayOneShot(clip, 0.75f);
            }
        }

        private void InicializarAudio()
        {
            musicaSource = gameObject.AddComponent<AudioSource>();
            musicaSource.loop = true;
            musicaSource.playOnAwake = false;
            musicaSource.volume = 0.16f;
            musicaSource.clip = CrearMusicaMedieval();
            musicaSource.Play();

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            sfxClips["jump"] = CrearTono(260f, 520f, 0.14f, 0.45f, false);
            sfxClips["spin"] = CrearTono(420f, 180f, 0.18f, 0.4f, true);
            sfxClips["crate"] = CrearTono(160f, 70f, 0.12f, 0.55f, true);
            sfxClips["wumpa"] = CrearTono(620f, 940f, 0.1f, 0.4f, false);
            sfxClips["tick"] = CrearTono(980f, 980f, 0.06f, 0.35f, false);
            sfxClips["explosion"] = CrearTono(140f, 38f, 0.42f, 0.7f, true);
            sfxClips["hit"] = CrearTono(220f, 95f, 0.22f, 0.55f, true);
            sfxClips["checkpoint"] = CrearTono(523f, 784f, 0.24f, 0.5f, false);
            sfxClips["victory"] = CrearFanfarria();
        }

        private AudioClip CrearTono(float freqInicio, float freqFin, float duracion, float amplitud, bool conRuido)
        {
            int sampleRate = 22050;
            int muestras = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duracion));
            float[] datos = new float[muestras];
            float fase = 0f;

            for (int i = 0; i < muestras; i++)
            {
                float t = (float)i / muestras;
                float freq = Mathf.Lerp(freqInicio, freqFin, t);
                fase += (2f * Mathf.PI * freq) / sampleRate;
                float env = 1f - t;
                float onda = Mathf.Sin(fase);
                if (conRuido)
                {
                    float ruido = Mathf.Sin(i * 12.9898f) * 0.45f;
                    onda = onda * 0.55f + ruido;
                }
                datos[i] = onda * env * amplitud;
            }

            AudioClip clip = AudioClip.Create("SFX", muestras, 1, sampleRate, false);
            clip.SetData(datos, 0);
            return clip;
        }

        private AudioClip CrearFanfarria()
        {
            int sampleRate = 22050;
            float[] notas = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int muestrasNota = Mathf.RoundToInt(sampleRate * 0.14f);
            float[] datos = new float[muestrasNota * notas.Length];
            float fase = 0f;

            for (int n = 0; n < notas.Length; n++)
            {
                for (int i = 0; i < muestrasNota; i++)
                {
                    float t = (float)i / muestrasNota;
                    fase += (2f * Mathf.PI * notas[n]) / sampleRate;
                    float env = 1f - (t * 0.6f);
                    datos[n * muestrasNota + i] = (Mathf.Sin(fase) * 0.7f + Mathf.Sin(fase * 2f) * 0.2f) * env * 0.45f;
                }
            }

            AudioClip clip = AudioClip.Create("VictorySFX", datos.Length, 1, sampleRate, false);
            clip.SetData(datos, 0);
            return clip;
        }

        private AudioClip CrearMusicaMedieval()
        {
            int sampleRate = 22050;
            //Generamos un sonido suave de fondo en bucle
            float[] notas = { 293.66f, 349.23f, 440.00f, 392.00f, 329.63f, 349.23f, 293.66f, 261.63f };
            float durNota = 0.55f;
            int muestrasNota = Mathf.RoundToInt(sampleRate * durNota);
            float[] datos = new float[muestrasNota * notas.Length];
            float fase = 0f;

            for (int n = 0; n < notas.Length; n++)
            {
                for (int i = 0; i < muestrasNota; i++)
                {
                    float t = (float)i / muestrasNota;
                    fase += (2f * Mathf.PI * notas[n]) / sampleRate;
                    float env = Mathf.Sin(t * Mathf.PI);
                    float laud = Mathf.Sin(fase) * 0.65f + Mathf.Sin(fase * 2f) * 0.2f + Mathf.Sin(fase * 0.5f) * 0.15f;
                    datos[n * muestrasNota + i] = laud * env * 0.25f;
                }
            }

            AudioClip clip = AudioClip.Create("MedievalLoop", datos.Length, 1, sampleRate, false);
            clip.SetData(datos, 0);
            return clip;
        }

        private Font ObtenerFuenteSegura()
        {
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fuente == null)
            {
                fuente = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            return fuente;
        }

        private void InicializarUI()
        {
            if (canvasHud == null)
            {
                GameObject canvasObj = new GameObject("HUD_GeeWiz");
                canvasHud = canvasObj.AddComponent<Canvas>();
                canvasHud.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                //Mitad ancho y mitad alto para que no se encime en pantallas angostas
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (FindAnyObjectByType<EventSystem>()== null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }

            Font fuente = ObtenerFuenteSegura();
            Transform root = canvasHud.transform;

            GameObject barraSup = CrearPanel(root, "BarraSuperior", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 54f), new Color(0.08f, 0.07f, 0.12f, 0.72f));
            HorizontalLayoutGroup hlg = barraSup.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 6, 6);
            hlg.spacing = 10f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            txtVidas = CrearTexto(barraSup.transform, fuente, 18, TextAnchor.MiddleLeft, Color.white);
            txtAkuAku = CrearTexto(barraSup.transform, fuente, 18, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.3f));
            txtWumpas = CrearTexto(barraSup.transform, fuente, 18, TextAnchor.MiddleLeft, new Color(1f, 0.68f, 0.2f));
            txtCajas = CrearTexto(barraSup.transform, fuente, 18, TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.55f));
            txtGema = CrearTexto(barraSup.transform, fuente, 18, TextAnchor.MiddleRight, new Color(0.55f, 0.9f, 1f));
            txtTiempo = CrearTexto(barraSup.transform, fuente, 18, TextAnchor.MiddleRight, Color.white);
            foreach (Text t in new[] { txtVidas, txtAkuAku, txtWumpas, txtCajas, txtGema, txtTiempo })
            {
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 11;
                t.resizeTextMaxSize = 18;
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Truncate;
            }

            GameObject barraInf = CrearPanel(root, "BarraControles", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 34f), new Color(0.05f, 0.05f, 0.08f, 0.6f));
            Text txtControles = CrearTexto(barraInf.transform, fuente, 14, TextAnchor.MiddleCenter, new Color(0.88f, 0.88f, 0.92f));
            EstirarRect(txtControles.rectTransform);
            txtControles.resizeTextForBestFit = true;
            txtControles.resizeTextMinSize = 10;
            txtControles.resizeTextMaxSize = 14;
            txtControles.verticalOverflow = VerticalWrapMode.Truncate;
            txtControles.text = "WASD: Mover | Shift: Correr | Espacio: Saltar x2 | Ctrl: Planchazo | Clic Izq/E: Giro | Clic Der/F: Lanzar | Esc: Pausa";

            panelPausa = CrearModal(root, fuente, "PAUSA", "Juego en pausa", out _, "Continuar", AlternarPausa, "Reiniciar", ReiniciarNivel, "Menu Principal", IrAlMenu);
            panelGameOver = CrearModal(root, fuente, "FIN DEL JUEGO", "Te has quedado sin vidas", out _, "Reintentar", ReiniciarNivel, "Menu Principal", IrAlMenu, null, null);
            panelVictoria = CrearModal(root, fuente, "NIVEL COMPLETADO", "", out txtResumenVictoria, "Jugar de nuevo", ReiniciarNivel, "Menu Principal", IrAlMenu, null, null);
        }

        private GameObject CrearPanel(Transform padre, string nombre, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(nombre);
            obj.transform.SetParent(padre, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private Text CrearTexto(Transform padre, Font fuente, int tamano, TextAnchor alineacion, Color color)
        {
            GameObject obj = new GameObject("Texto");
            obj.transform.SetParent(padre, false);
            obj.AddComponent<RectTransform>();
            Text t = obj.AddComponent<Text>();
            t.font = fuente;
            t.fontSize = tamano;
            t.alignment = alineacion;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private GameObject CrearModal(Transform padre, Font fuente, string titulo, string subtitulo, out Text txtCuerpo,
            string textoBtn1, UnityEngine.Events.UnityAction acc1,
            string textoBtn2, UnityEngine.Events.UnityAction acc2,
            string textoBtn3, UnityEngine.Events.UnityAction acc3)
        {
            GameObject fondo = CrearPanel(padre, "Panel_" + titulo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.68f));
            GameObject caja = CrearPanel(fondo.transform, "Caja", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 0f), new Color(0.14f, 0.12f, 0.2f, 0.96f));
            //La caja crece segun lo que tenga adentro para que los botones no se salgan
            caja.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup vlg = caja.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            Text tTitulo = CrearTexto(caja.transform, fuente, 26, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f));
            tTitulo.text = titulo;
            tTitulo.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

            txtCuerpo = CrearTexto(caja.transform, fuente, 17, TextAnchor.MiddleCenter, Color.white);
            txtCuerpo.text = subtitulo;
            txtCuerpo.gameObject.AddComponent<LayoutElement>().preferredHeight = string.IsNullOrEmpty(subtitulo) ? 72f : 36f;

            if (!string.IsNullOrEmpty(textoBtn1)) CrearBoton(caja.transform, fuente, textoBtn1, acc1);
            if (!string.IsNullOrEmpty(textoBtn2)) CrearBoton(caja.transform, fuente, textoBtn2, acc2);
            if (!string.IsNullOrEmpty(textoBtn3)) CrearBoton(caja.transform, fuente, textoBtn3, acc3);

            fondo.SetActive(false);
            return fondo;
        }

        private void CrearBoton(Transform padre, Font fuente, string etiqueta, UnityEngine.Events.UnityAction accion)
        {
            GameObject btnObj = new GameObject("Btn_" + etiqueta);
            btnObj.transform.SetParent(padre, false);
            btnObj.AddComponent<RectTransform>();
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.28f, 0.45f, 0.72f, 1f);
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 44f;

            Button btn = btnObj.AddComponent<Button>();
            if (accion != null) btn.onClick.AddListener(accion);

            Text t = CrearTexto(btnObj.transform, fuente, 18, TextAnchor.MiddleCenter, Color.white);
            t.text = etiqueta;
            EstirarRect(t.rectTransform);
        }

        private void EstirarRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ActualizarHUD()
        {
            if (txtVidas != null) txtVidas.text = $"Vidas: {vidas}";
            if (txtAkuAku != null) txtAkuAku.text = $"Aku Aku x{escudosAku}";
            if (txtWumpas != null) txtWumpas.text = $"Wumpas: {wumpas}";
            if (txtCajas != null) txtCajas.text = $"Cajas: {cajasRotas} / {cajasTotales}";
            if (txtGema != null)
            {
                string estadoCristal = cristalObtenido ? "OK" : "--";
                string estadoGema = gemaObtenida ? "OK" : "--";
                txtGema.text = $"Cristal: {estadoCristal} / Gema: {estadoGema}";
            }

            if (txtTiempo != null)
            {
                if (!timeTrialActivo)
                {
                    txtTiempo.text = "Tiempo: --:--";
                }
                else if (tiempoCongeladoRestante > 0f)
                {
                    txtTiempo.text = $"Tiempo: {FormatearTiempo(tiempoTranscurrido)} [+{tiempoCongeladoRestante:0.0}s]";
                }
                else
                {
                    txtTiempo.text = $"Tiempo: {FormatearTiempo(tiempoTranscurrido)}";
                }
            }
        }

        private string FormatearTiempo(float segundos)
        {
            int mins = Mathf.FloorToInt(segundos / 60f);
            float segs = segundos % 60f;
            return $"{mins:00}:{segs:00.0}";
        }
    }
}
