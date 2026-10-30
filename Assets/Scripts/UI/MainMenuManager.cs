using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GyroPilot.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        private enum MenuState { Main, Settings, HowToPlay, Credits }
        private MenuState currentState = MenuState.Main;

        private Vector2 settingsScrollPos = Vector2.zero;
        private Vector2 helpScrollPos = Vector2.zero;
        private Vector2 creditsScrollPos = Vector2.zero;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle badgeStyle;
        private GUIStyle btnPrimaryGreenStyle;
        private GUIStyle btnPrimaryCyanStyle;
        private GUIStyle btnSecondaryStyle;
        private GUIStyle btnGoldStyle;
        private GUIStyle btnActiveStyle;
        private GUIStyle labelStyle;
        private GUIStyle wrappedLabelStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle panelStyle;
        private GUIStyle creditRoleStyle;
        private GUIStyle creditNameStyle;
        private GUIStyle creditDedicationStyle;
        private Texture2D darkTex;
        private Texture2D panelBorderTex;
        private Texture2D blueTex;
        private Texture2D greenTex;
        private Texture2D cyanTex;
        private Texture2D goldTex;
        private Texture2D cardBgTex;
        private Texture2D bgGradientTex;
        private bool isStylesInit = false;

        private void Awake()
        {
            SettingsManager.Initialize();
        }

        private void InitStyles()
        {
            if (isStylesInit) return;

            darkTex = MakeTex(2, 2, new Color(0.05f, 0.07f, 0.11f, 0.94f));
            panelBorderTex = MakeBorderedTex(128, 64, new Color(0.04f, 0.06f, 0.09f, 0.95f), new Color(0.25f, 0.65f, 1.0f, 0.65f), 2);
            blueTex = MakeTex(2, 2, new Color(0.12f, 0.42f, 0.85f, 0.95f));
            greenTex = MakeTex(2, 2, new Color(0.10f, 0.70f, 0.32f, 0.95f));
            cyanTex = MakeTex(2, 2, new Color(0.08f, 0.65f, 0.85f, 0.95f));
            goldTex = MakeTex(2, 2, new Color(0.85f, 0.65f, 0.12f, 0.95f));
            cardBgTex = MakeTex(2, 2, new Color(0.08f, 0.11f, 0.16f, 0.90f));

            bgGradientTex = new Texture2D(1, 256);
            for (int y = 0; y < 256; y++)
            {
                float t = y / 255f;
                Color col = Color.Lerp(new Color(0.03f, 0.04f, 0.07f), new Color(0.09f, 0.13f, 0.20f), t);
                bgGradientTex.SetPixel(0, y, col);
            }
            bgGradientTex.Apply();

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelBorderTex, textColor = Color.white },
                padding = new RectOffset(25, 25, 25, 25)
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 50,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.85f, 1.0f) },
                alignment = TextAnchor.MiddleCenter
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.90f, 0.96f) },
                alignment = TextAnchor.MiddleCenter
            };

            badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 0.75f, 0.95f) },
                alignment = TextAnchor.MiddleCenter
            };

            sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.85f, 1.0f) },
                alignment = TextAnchor.MiddleCenter
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft
            };

            wrappedLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.90f, 0.92f, 0.96f) },
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };

            btnPrimaryGreenStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = greenTex, textColor = Color.white },
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            btnPrimaryCyanStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = cyanTex, textColor = Color.white },
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            btnGoldStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = goldTex, textColor = Color.white },
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            btnSecondaryStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = darkTex, textColor = Color.white },
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            btnActiveStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = blueTex, textColor = Color.white },
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            creditRoleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.85f, 1.0f) },
                alignment = TextAnchor.MiddleCenter
            };

            creditNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            creditDedicationStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.88f, 0.30f) },
                alignment = TextAnchor.MiddleCenter
            };

            isStylesInit = true;
        }

        private Texture2D MakeBorderedTex(int w, int h, Color bg, Color border, int b)
        {
            Color[] pix = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x < b || x >= w - b || y < b || y >= h - b);
                    bool isTopAccent = (y >= h - b * 2);
                    if (isTopAccent) pix[y * w + x] = border;
                    else if (isBorder) pix[y * w + x] = border * 0.7f;
                    else pix[y * w + x] = bg;
                }
            }
            Texture2D tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            // Si la escena activa ya no es MainMenu, destruir este objeto para que no cubra la pista
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                Destroy(this.gameObject);
                return;
            }

            InitStyles();

            // Fondo completo gradiente oscuro deportivo
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bgGradientTex);

            // Cabecera Principal de Competición
            float headerY = Mathf.Max(20f, Screen.height * 0.05f);
            GUI.Label(new Rect(0, headerY, Screen.width, 58), "GYROPILOT 360", titleStyle);
            GUI.Label(new Rect(0, headerY + 56, Screen.width, 24), "SIMULADOR DE AUTOMOVILISMO DEPORTIVO EN PRIMERA PERSONA", subtitleStyle);
            GUI.Label(new Rect(0, headerY + 80, Screen.width, 20), "TELEMETRÍA 60 FPS • VOLANTE POR GIROSCOPIO MÓVIL • FÍSICA MECÁNICA REAL", badgeStyle);

            switch (currentState)
            {
                case MenuState.Main:
                    DrawMainMenu();
                    break;
                case MenuState.Settings:
                    DrawSettingsMenu();
                    break;
                case MenuState.HowToPlay:
                    DrawHowToPlay();
                    break;
                case MenuState.Credits:
                    DrawCreditsMenu();
                    break;
            }

            // Pie de página pro
            GUI.Label(new Rect(25, Screen.height - 30, Screen.width - 50, 22), "GyroPilot 360 Pro Edition • Colegio Carlos Vicente Rey • Unity 6", badgeStyle);
        }

        private void DrawMainMenu()
        {
            float panelW = Mathf.Clamp(Screen.width * 0.48f, 500f, 640f);
            float panelH = 460f;
            float panelX = (Screen.width - panelW) * 0.5f;
            float panelY = (Screen.height - panelH) * 0.54f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", panelStyle);

            float btnW = panelW - 50f;
            float btnH = 46f;
            float btnX = panelX + 25f;
            float startY = panelY + 24f;
            float gap = 54f;

            // 1. MODO CIRCUITO GP
            if (GUI.Button(new Rect(btnX, startY, btnW, btnH), "🏁  MODO CIRCUITO GP (PISTA CERRADA)", btnPrimaryGreenStyle))
            {
                LaunchSimulator("SampleScene");
            }

            // 2. RECTA INFINITA CON OBSTÁCULOS
            if (GUI.Button(new Rect(btnX, startY + gap, btnW, btnH), "⚡  RECTA INFINITA (TEST VELOCIDAD 300+)", btnPrimaryCyanStyle))
            {
                LaunchSimulator("InfiniteDragStrip");
            }

            // 3. CONFIGURACIÓN
            if (GUI.Button(new Rect(btnX, startY + gap * 2, btnW, btnH), "⚙  CONFIGURACIÓN Y AJUSTES", btnSecondaryStyle))
            {
                currentState = MenuState.Settings;
            }

            // 4. CÓMO JUGAR / CONEXIÓN
            if (GUI.Button(new Rect(btnX, startY + gap * 3, btnW, btnH), "📱  CÓMO CONECTAR EL CELULAR", btnSecondaryStyle))
            {
                currentState = MenuState.HowToPlay;
            }

            // 5. CRÉDITOS DEL PROYECTO
            if (GUI.Button(new Rect(btnX, startY + gap * 4, btnW, btnH), "🏆  CRÉDITOS DEL PROYECTO", btnGoldStyle))
            {
                currentState = MenuState.Credits;
            }

            // 6. SALIR
            if (GUI.Button(new Rect(btnX, startY + gap * 5, btnW, btnH), "✕  SALIR AL ESCRITORIO", btnSecondaryStyle))
            {
                Application.Quit();
            }
        }

        private void DrawCreditsMenu()
        {
            float panelW = Mathf.Clamp(Screen.width * 0.74f, 640f, 900f);
            float panelH = Mathf.Clamp(Screen.height * 0.78f, 520f, 680f);
            float panelX = (Screen.width - panelW) * 0.5f;
            float panelY = (Screen.height - panelH) * 0.54f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", panelStyle);

            float viewW = panelW - 50f;
            float viewH = panelH - 95f;

            Rect scrollRect = new Rect(panelX + 25f, panelY + 20f, viewW, viewH);
            Rect contentRect = new Rect(0, 0, viewW - 25f, 540f);

            creditsScrollPos = GUI.BeginScrollView(scrollRect, creditsScrollPos, contentRect);

            float curY = 5f;
            float contentW = contentRect.width;

            GUI.Label(new Rect(0, curY, contentW, 32), "🏆 CRÉDITOS Y RECONOCIMIENTOS", sectionHeaderStyle);
            curY += 36f;

            GUI.Label(new Rect(0, curY, contentW, 20), "SIMULADOR DE AUTOMOVILISMO TELEMÉTRICO PROFESIONAL", badgeStyle);
            curY += 35f;

            // --- SECCIÓN: INSTITUCIÓN Y PROFESOR ---
            float boxH1 = 95f;
            GUI.Box(new Rect(0, curY, contentW, boxH1), "", panelStyle);
            GUI.Label(new Rect(10, curY + 10f, contentW - 20, 20), "PROYECTO DIRIGIDO A:", creditRoleStyle);
            GUI.Label(new Rect(10, curY + 32f, contentW - 20, 28), "🏫 COLEGIO CARLOS VICENTE REY", creditDedicationStyle);
            GUI.Label(new Rect(10, curY + 62f, contentW - 20, 24), "Profesor Asesor: Ronald Barrera", creditNameStyle);
            curY += boxH1 + 25f;

            // --- SECCIÓN: AUTORES Y DESARROLLADORES ---
            float boxH2 = 175f;
            GUI.Box(new Rect(0, curY, contentW, boxH2), "", panelStyle);
            GUI.Label(new Rect(10, curY + 12f, contentW - 20, 20), "EQUIPO DE DESARROLLO Y AUTORES:", creditRoleStyle);

            float authorCardW = (contentW - 30f) / 3f;
            string[] authors = new string[]
            {
                "Oscar Mauricio\nSandoval Márquez",
                "Joseph Sneijder\nDelgado Tarazona",
                "Kevin Andrés\nCorrea Martinez"
            };

            for (int i = 0; i < 3; i++)
            {
                float cardX = 10f + i * (authorCardW + 5f);
                float cardY = curY + 42f;
                Rect cardRect = new Rect(cardX, cardY, authorCardW, 110f);
                GUI.Box(cardRect, "", panelStyle);

                GUI.Label(new Rect(cardX, cardY + 14f, authorCardW, 20), $"🏎️ PILOTO {i + 1}", creditRoleStyle);
                GUI.Label(new Rect(cardX + 5f, cardY + 40f, authorCardW - 10f, 55f), authors[i], creditNameStyle);
            }
            curY += boxH2 + 25f;

            // --- SECCIÓN: DETALLES TÉCNICOS ---
            float boxH3 = 105f;
            GUI.Box(new Rect(0, curY, contentW, boxH3), "", panelStyle);
            GUI.Label(new Rect(15, curY + 10f, contentW - 30, 20), "CARACTERÍSTICAS TÉCNICAS DEL SISTEMA:", creditRoleStyle);
            string techDetails =
                "• Volante de carreras controlado en tiempo real vía giroscopio móvil y WebSockets a 60 FPS.\n" +
                "• Transmisión manual en H con corte de encendido (Flatshift de 150ms), freno motor dinámico y Rev Limiter.\n" +
                "• Tacómetro y velocímetro análogos circulares de alta precisión con aguja deportiva y HUD telemétrico.\n" +
                "• Circuito cerrado tipo Gran Premio con cronometraje y Autopista infinita para prueba de velocidad punta.";
            GUI.Label(new Rect(15, curY + 32f, contentW - 30, 68), techDetails, wrappedLabelStyle);
            curY += boxH3 + 30f;

            GUI.EndScrollView();

            // Botón Volver fijo
            if (GUI.Button(new Rect(panelX + (panelW - 240f) * 0.5f, panelY + panelH - 62f, 240f, 44f), "◀ VOLVER AL MENÚ", btnActiveStyle))
            {
                currentState = MenuState.Main;
            }
        }

        private void LaunchSimulator(string sceneName)
        {
            Time.timeScale = 1f;
            enabled = false; // Desactivar OnGUI al instante para dar paso al HUD
            Debug.Log($"GyroPilot: Cargando {sceneName}...");

#if UNITY_EDITOR
            // Si estamos probando en el Editor de Unity, carga directa de escena garantizada
            string editorScenePath = $"Assets/Scenes/{sceneName}.unity";
            if (System.IO.File.Exists(editorScenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    editorScenePath,
                    new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)
                );
                return;
            }
#endif

            // En compilación Standalone (.exe)
            try
            {
                SceneManager.LoadScene(sceneName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Intento por nombre '{sceneName}' falló ({ex.Message}). Cargando por índice...");
                try
                {
                    int targetIdx = (sceneName == "InfiniteDragStrip") ? 2 : 1;
                    SceneManager.LoadScene(targetIdx);
                }
                catch (Exception ex2)
                {
                    Debug.LogError("Error al cargar escena del juego: " + ex2.Message);
                }
            }
        }

        private void DrawSettingsMenu()
        {
            float panelW = Mathf.Clamp(Screen.width * 0.70f, 600f, 850f);
            float panelH = Mathf.Clamp(Screen.height * 0.72f, 440f, 620f);
            float panelX = (Screen.width - panelW) * 0.5f;
            float panelY = (Screen.height - panelH) * 0.52f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", panelStyle);

            float viewW = panelW - 50f;
            float viewH = panelH - 95f;

            // ScrollView para evitar cualquier recorte de textos
            Rect scrollRect = new Rect(panelX + 25f, panelY + 20f, viewW, viewH);
            Rect contentRect = new Rect(0, 0, viewW - 25f, 440f);

            settingsScrollPos = GUI.BeginScrollView(scrollRect, settingsScrollPos, contentRect);

            float curY = 5f;
            float contentW = contentRect.width;

            GUI.Label(new Rect(0, curY, contentW, 30), "⚙ CONFIGURACIÓN GENERAL", sectionHeaderStyle);
            curY += 45f;

            // --- Calidad Gráfica ---
            GUI.Label(new Rect(0, curY, contentW, 24), "CALIDAD GRÁFICA:", labelStyle);
            curY += 30f;

            string[] qualityNames = QualitySettings.names;
            if (qualityNames == null || qualityNames.Length == 0)
            {
                qualityNames = new string[] { "Baja", "Media", "Alta", "Ultra" };
            }

            int currentQuality = QualitySettings.GetQualityLevel();
            float qBtnW = (contentW - (qualityNames.Length - 1) * 8f) / qualityNames.Length;

            for (int i = 0; i < qualityNames.Length; i++)
            {
                GUIStyle qStyle = (i == currentQuality) ? btnActiveStyle : btnSecondaryStyle;
                if (GUI.Button(new Rect(i * (qBtnW + 8f), curY, qBtnW, 40), qualityNames[i], qStyle))
                {
                    SettingsManager.SetQualityLevel(i);
                }
            }
            curY += 58f;

            // --- Volumen de Sonido ---
            float vol = SettingsManager.GetMasterVolume();
            int volPercent = Mathf.RoundToInt(vol * 100f);
            GUI.Label(new Rect(0, curY, contentW, 24), $"VOLUMEN MAESTRO: {volPercent}%", labelStyle);
            curY += 30f;

            if (GUI.Button(new Rect(0, curY, 50, 36), " - ", btnSecondaryStyle))
            {
                SettingsManager.SetMasterVolume(Mathf.Max(0f, vol - 0.1f));
            }

            float newVol = GUI.HorizontalSlider(new Rect(60, curY + 10, contentW - 120, 20), vol, 0f, 1f);
            if (Mathf.Abs(newVol - vol) > 0.01f)
            {
                SettingsManager.SetMasterVolume(newVol);
            }

            if (GUI.Button(new Rect(contentW - 50, curY, 50, 36), " + ", btnSecondaryStyle))
            {
                SettingsManager.SetMasterVolume(Mathf.Min(1f, vol + 0.1f));
            }
            curY += 60f;

            // --- Inversión de Giro ---
            bool isInv = SettingsManager.GetInvertSteering(true);
            string invText = isInv ? "INVERTIDO: SÍ (Recomendado para Firefox Móvil)" : "INVERTIDO: NO (Normal)";
            GUI.Label(new Rect(0, curY, contentW, 24), "DIRECCIÓN DEL VOLANTE:", labelStyle);
            curY += 30f;

            if (GUI.Button(new Rect(0, curY, contentW, 42), invText, btnSecondaryStyle))
            {
                SettingsManager.SetInvertSteering(!isInv);
            }
            curY += 60f;

            GUI.EndScrollView();

            // Botón Volver fijo en la parte inferior del panel
            if (GUI.Button(new Rect(panelX + (panelW - 220f) * 0.5f, panelY + panelH - 60f, 220f, 44f), "◀ VOLVER AL MENÚ", btnActiveStyle))
            {
                currentState = MenuState.Main;
            }
        }

        private void DrawHowToPlay()
        {
            float panelW = Mathf.Clamp(Screen.width * 0.75f, 620f, 900f);
            float panelH = Mathf.Clamp(Screen.height * 0.75f, 460f, 660f);
            float panelX = (Screen.width - panelW) * 0.5f;
            float panelY = (Screen.height - panelH) * 0.52f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", panelStyle);

            float viewW = panelW - 50f;
            float viewH = panelH - 95f;

            // ScrollView interactivo
            Rect scrollRect = new Rect(panelX + 25f, panelY + 20f, viewW, viewH);
            Rect contentRect = new Rect(0, 0, viewW - 25f, 480f);

            helpScrollPos = GUI.BeginScrollView(scrollRect, helpScrollPos, contentRect);

            float curY = 5f;
            float contentW = contentRect.width;

            GUI.Label(new Rect(0, curY, contentW, 30), "📱 GUÍA DE CONEXIÓN DEL VOLANTE MÓVIL", sectionHeaderStyle);
            curY += 45f;

            string guideP1 =
                "1. CONEXIÓN POR CABLE USB (Recomendado para cero latencia):\n" +
                "   • Conecta el cable USB del celular a tu computador.\n" +
                "   • En los ajustes de tu celular, entra a 'Redes / Conexión' y activa 'Anclaje a red USB' (USB Tethering).\n\n" +
                "2. CONEXIÓN POR WI-FI:\n" +
                "   • Si prefieres sin cables, simplemente conecta el celular a la misma red Wi-Fi o Hotspot del PC.\n\n" +
                "3. NAVEGADOR FIREFOX EN EL CELULAR:\n" +
                "   • Abre Firefox en tu celular (Chrome restringe giroscopio en redes locales).\n" +
                "   • Ingresa la dirección IP que aparece en pantalla (ej: http://192.168.x.x:8090) o escanea el código QR que se muestra en la esquina del juego.\n\n" +
                "4. ¡A CORRER!\n" +
                "   • Sujeta el celular en horizontal como un volante de carreras real.\n" +
                "   • Pedales a la izquierda (Acelerador y Freno) y palanca de cambios en H a la derecha.\n\n" +
                "CONTROLES DE RESPALDO CON TECLADO:\n" +
                "• Girar: A / D o Flechas Izquierda / Derecha\n" +
                "• Acelerar / Frenar: W / S o Flechas Arriba / Abajo\n" +
                "• Marchas: Teclas 1 a 5 y R (Reversa)\n" +
                "• Encender / Apagar Motor: Tecla E\n" +
                "• Menú de Pausa: Tecla ESC";

            GUI.Label(new Rect(0, curY, contentW, 420f), guideP1, wrappedLabelStyle);
            curY += 430f;

            GUI.EndScrollView();

            // Botón Volver fijo
            if (GUI.Button(new Rect(panelX + (panelW - 220f) * 0.5f, panelY + panelH - 60f, 220f, 44f), "◀ ENTENDIDO", btnActiveStyle))
            {
                currentState = MenuState.Main;
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
