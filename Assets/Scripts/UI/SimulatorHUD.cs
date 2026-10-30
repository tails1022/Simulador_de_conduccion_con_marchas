using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using GyroPilot.Networking;
using GyroPilot.Vehicle;
using GyroPilot.Track;

namespace GyroPilot.UI
{
    public class SimulatorHUD : MonoBehaviour
    {
        [Header("Estado de Pausa y Menús")]
        public bool isPaused = false;
        private bool showingSettings = false;
        private bool forceShowFullQr = false;
        private Vector2 pauseScrollPos = Vector2.zero;

        private Texture2D qrTexture;
        private Texture2D tachDialTex;
        private Texture2D speedDialTex;
        private Texture2D tachNeedleTex;
        private Texture2D speedNeedleTex;
        private Texture2D dialCapTex;
        private Texture2D dimTex;
        private Texture2D darkTex;
        private Texture2D blueTex;
        private Texture2D greenTex;
        private Texture2D redTex;
        private Texture2D greyTex;

        private GUIStyle titleStyle;
        private GUIStyle gearStyle;
        private GUIStyle speedStyle;
        private GUIStyle rpmStyle;
        private GUIStyle dialLabelStyle;
        private GUIStyle warningStyle;
        private GUIStyle carBtnStyle;
        private GUIStyle carBtnActiveStyle;
        private GUIStyle boxStyle;
        private GUIStyle engineOnBtnStyle;
        private GUIStyle engineOffBtnStyle;
        private GUIStyle pauseTitleStyle;
        private GUIStyle pauseBtnPrimaryStyle;
        private GUIStyle pauseBtnSecondaryStyle;
        private bool isStylesInit = false;

        private void Awake()
        {
            SettingsManager.Initialize();
        }

        private void Start()
        {
            GenerateQR();
            if (TelemetryServer.Instance != null)
            {
                TelemetryServer.Instance.OnServerUrlChanged += GenerateQR;
            }

            BuildProceduralDialTextures();
        }

        private void OnDestroy()
        {
            if (TelemetryServer.Instance != null)
            {
                TelemetryServer.Instance.OnServerUrlChanged -= GenerateQR;
            }
        }

        private void Update()
        {
            // Detección de Escape para pausar / reanudar
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            if (!isPaused)
            {
                showingSettings = false;
            }
        }

        private void GenerateQR()
        {
            string url = "http://127.0.0.1:8090/";
            if (TelemetryServer.Instance != null && !string.IsNullOrEmpty(TelemetryServer.Instance.ServerUrl))
            {
                url = TelemetryServer.Instance.ServerUrl;
            }
            qrTexture = QRCodeUtility.GenerateQRCodeTexture(url, 4);
        }

        private void BuildProceduralDialTextures()
        {
            tachDialTex = CreateDialTexture(256, isTachometer: true);
            speedDialTex = CreateDialTexture(256, isTachometer: false);
            tachNeedleTex = CreateNeedleTexture(12, 105, new Color(1.0f, 0.28f, 0.15f, 0.95f)); // Naranja deportivo
            speedNeedleTex = CreateNeedleTexture(12, 105, new Color(0.25f, 0.75f, 1.0f, 0.95f)); // Azul celeste hielo
            dialCapTex = CreateCapTexture(46);
        }

        private void InitStyles()
        {
            if (isStylesInit) return;

            darkTex = MakeTex(2, 2, new Color(0.06f, 0.08f, 0.12f, 0.88f));
            redTex = MakeTex(2, 2, new Color(0.85f, 0.15f, 0.15f, 0.95f));
            blueTex = MakeTex(2, 2, new Color(0.15f, 0.45f, 0.85f, 0.92f));
            greenTex = MakeTex(2, 2, new Color(0.12f, 0.72f, 0.32f, 0.92f));
            greyTex = MakeTex(2, 2, new Color(0.18f, 0.22f, 0.28f, 0.85f));
            dimTex = MakeTex(2, 2, new Color(0.02f, 0.03f, 0.05f, 0.75f));

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = darkTex, textColor = Color.white },
                padding = new RectOffset(10, 10, 10, 10)
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.75f, 1.0f) },
                alignment = TextAnchor.UpperLeft
            };

            gearStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.2f) },
                alignment = TextAnchor.MiddleCenter
            };

            speedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            rpmStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.75f, 0.80f, 0.85f) },
                alignment = TextAnchor.MiddleCenter
            };

            dialLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.88f, 0.92f) },
                alignment = TextAnchor.MiddleCenter
            };

            warningStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = redTex, textColor = Color.white },
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            carBtnStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = greyTex, textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            carBtnActiveStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = blueTex, textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            engineOnBtnStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = greenTex, textColor = Color.white },
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            engineOffBtnStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = redTex, textColor = Color.white },
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            pauseTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.78f, 1.0f) },
                alignment = TextAnchor.MiddleCenter
            };

            pauseBtnPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = greenTex, textColor = Color.white },
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            pauseBtnSecondaryStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = darkTex, textColor = Color.white },
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            isStylesInit = true;
        }

        private CarPhysicsController GetActiveCar()
        {
            CarPhysicsController car = null;
            if (CarSelectionManager.Instance != null && CarSelectionManager.Instance.ActiveCar != null)
            {
                car = CarSelectionManager.Instance.ActiveCar;
            }
            if (car == null)
            {
                car = FindAnyObjectByType<CarPhysicsController>();
            }
            return car;
        }

        private void OnGUI()
        {
            InitStyles();

            // Garantizar que las texturas procedurales existan siempre (incluso tras recargar escena)
            if (tachDialTex == null || speedDialTex == null || tachNeedleTex == null || speedNeedleTex == null)
            {
                BuildProceduralDialTextures();
            }

            CarPhysicsController car = GetActiveCar();
            bool isPhoneConnected = TelemetryServer.Instance != null && TelemetryServer.Instance.IsClientConnected;

            // 1. INSTRUMENTOS PRINCIPALES (Tacómetro y Velocímetro Circulares en la base)
            DrawCircularDashboard(car);

            // 2. PANEL DE CONEXIÓN CELULAR & QR (Esquina Superior Derecha)
            DrawConnectionPanel(isPhoneConnected);

            // 3. SELECTOR DE 4 AUTOS (Esquina Superior Izquierda)
            DrawCarSelector();

            // 4. CRONÓMETRO DE VUELTA (Centro Superior)
            DrawLapTimer();

            // 5. ALERTA DE EMBRAGUE O MOTOR AHOGADO (Centro Pantalla)
            if (car != null && car.Gearbox != null)
            {
                float w = Mathf.Clamp(Screen.width * 0.65f, 560f, 720f);
                float h = 70f;
                Rect warnRect = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.36f, w, h);

                if (car.Gearbox.ShowClutchWarning)
                {
                    GUI.Box(warnRect, "⚠️ ¡SUELTA ACELERADOR PARA ENGRANAR!\n(Corte de 150ms aplicado)", warningStyle);
                }
                else if (car.Gearbox.IsEngineLugging)
                {
                    GUI.Box(warnRect, car.Gearbox.LuggingWarningMessage, warningStyle);
                }
            }

            // 6. MENÚ DE PAUSA MODAL (Si está pausado con ESC)
            if (isPaused)
            {
                DrawPauseMenu(car);
            }
        }

        /// <summary>
        /// Dibuja el cuadro de instrumentos deportivo con Tacómetro y Velocímetro circulares análogos.
        /// </summary>
        private void DrawCircularDashboard(CarPhysicsController car)
        {
            float dialSize = 185f;
            float centerBridgeW = 160f;
            float totalW = (dialSize * 2f) + centerBridgeW + 20f;
            float startX = (Screen.width - totalW) * 0.5f;
            float startY = Screen.height - dialSize - 18f;

            // Extraer telemetría de forma segura aunque el auto sea nulo
            float currentRpm = (car != null && car.Gearbox != null) ? car.Gearbox.CurrentRPM : 0f;
            float maxRpm = (car != null && car.Gearbox != null) ? car.Gearbox.MaxRPM : 8200f;
            float currentSpeed = (car != null) ? Mathf.Abs(car.currentSpeedKmh) : 0f;
            bool engineOn = (car != null) && car.isEngineRunning;
            float steerVal = (car != null) ? car.currentSteerInput : 0f;
            bool isInv = (car != null) ? car.invertSteering : SettingsManager.GetInvertSteering(true);

            string gearText = "N";
            if (car != null && car.Gearbox != null)
            {
                if (car.Gearbox.CurrentGear == -1) gearText = "R";
                else if (car.Gearbox.CurrentGear > 0) gearText = car.Gearbox.CurrentGear.ToString();
            }

            // --- A. TACÓMETRO CIRCULAR (Izquierda) ---
            Rect tachRect = new Rect(startX, startY, dialSize, dialSize);
            Vector2 tachCenter = new Vector2(tachRect.x + dialSize * 0.5f, tachRect.y + dialSize * 0.5f);

            // 1. Esfera del tacómetro
            if (tachDialTex != null) GUI.DrawTexture(tachRect, tachDialTex);

            // 2. Números radiales de RPM alrededor del tacómetro (0 a 8)
            DrawDialLabels(tachCenter, dialSize * 0.37f, new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8" }, -125f, 125f);

            // 3. Aguja rotada del tacómetro
            float rpmRatio = Mathf.Clamp01(currentRpm / maxRpm);
            float tachAngle = Mathf.Lerp(-125f, 125f, rpmRatio);

            DrawRotatedNeedle(tachCenter, tachAngle, dialSize * 0.42f, tachNeedleTex);

            // 4. Centro del tacómetro (Tapa central con Marcha actual y RPM numérico)
            if (dialCapTex != null)
            {
                float capSize = 72f;
                Rect capRect = new Rect(tachCenter.x - capSize * 0.5f, tachCenter.y - capSize * 0.5f, capSize, capSize);
                GUI.DrawTexture(capRect, dialCapTex);
            }

            GUI.Label(new Rect(tachCenter.x - 35f, tachCenter.y - 30f, 70f, 40f), gearText, gearStyle);
            GUI.Label(new Rect(tachCenter.x - 45f, tachCenter.y + 7f, 90f, 22f), $"{Mathf.RoundToInt(currentRpm)}", rpmStyle);


            // --- B. CONSOLA CENTRAL (Puente telemétrico entre ambos diales) ---
            float bridgeX = startX + dialSize + 10f;
            Rect bridgeRect = new Rect(bridgeX, startY + 8f, centerBridgeW, dialSize - 16f);
            GUI.Box(bridgeRect, "", boxStyle);

            // 1. Botón de Encendido / Apagado (Start / Stop Engine)
            string engText = (car == null) ? "○ SIN AUTO" : (engineOn ? "⚡ ENGINE ON" : "🛑 ENGINE OFF");
            GUIStyle engBtnStyle = engineOn ? engineOnBtnStyle : engineOffBtnStyle;

            if (GUI.Button(new Rect(bridgeX + 8f, bridgeRect.y + 8f, centerBridgeW - 16f, 32f), engText, engBtnStyle))
            {
                car?.ToggleEngine();
            }

            // 2. Telemetría de Pedales en tiempo real (Acelerador y Freno)
            float barW = (centerBridgeW - 24f) * 0.5f;
            float barH = 36f;
            float barY = bridgeRect.y + 44f;

            // Barra Acelerador (Verde dinámico)
            float throttleVal = (car != null) ? car.currentThrottleInput : 0f;
            GUI.Box(new Rect(bridgeX + 8f, barY, barW, barH), "", boxStyle);
            if (greenTex != null && throttleVal > 0.02f)
            {
                float fillH = (barH - 4f) * throttleVal;
                GUI.DrawTexture(new Rect(bridgeX + 10f, barY + barH - 2f - fillH, barW - 4f, fillH), greenTex);
            }
            GUI.Label(new Rect(bridgeX + 8f, barY + 9f, barW, 18f), "GAS", dialLabelStyle);

            // Barra Freno (Rojo dinámico)
            float brakeVal = (car != null) ? car.currentBrakeInput : 0f;
            GUI.Box(new Rect(bridgeX + 16f + barW, barY, barW, barH), "", boxStyle);
            if (redTex != null && brakeVal > 0.02f)
            {
                float fillH = (barH - 4f) * brakeVal;
                GUI.DrawTexture(new Rect(bridgeX + 18f + barW, barY + barH - 2f - fillH, barW - 4f, fillH), redTex);
            }
            GUI.Label(new Rect(bridgeX + 16f + barW, barY + 9f, barW, 18f), "FRENO", dialLabelStyle);

            // 3. Indicador de Giro en tiempo real con botón de inversión rápida
            string steerArrow = steerVal < -0.05f ? "◄ IZQ" : (steerVal > 0.05f ? "DER ►" : "● RECTO");
            string invHint = isInv ? " (Inv)" : "";

            if (GUI.Button(new Rect(bridgeX + 8f, bridgeRect.y + 86f, centerBridgeW - 16f, 26f), $"{steerArrow}{invHint}", carBtnStyle))
            {
                if (car != null) car.invertSteering = !car.invertSteering;
                SettingsManager.SetInvertSteering(!isInv);
            }

            // 4. Atajo de Pausa y Tecla de encendido
            GUI.Label(new Rect(bridgeX + 4f, bridgeRect.y + 116f, centerBridgeW - 8f, 18f), "Motor: [E]  |  Pausa: [ESC]", dialLabelStyle);


            // --- C. VELOCÍMETRO CIRCULAR (Derecha) ---
            float speedX = bridgeX + centerBridgeW + 10f;
            Rect speedRect = new Rect(speedX, startY, dialSize, dialSize);
            Vector2 speedCenter = new Vector2(speedRect.x + dialSize * 0.5f, speedRect.y + dialSize * 0.5f);

            // 1. Esfera del velocímetro
            if (speedDialTex != null) GUI.DrawTexture(speedRect, speedDialTex);

            // 2. Números radiales de KM/H alrededor del velocímetro
            DrawDialLabels(speedCenter, dialSize * 0.37f, new string[] { "0", "40", "80", "120", "160", "200", "240", "280", "320" }, -125f, 125f);

            // 3. Aguja rotada del velocímetro
            float speedRatio = Mathf.Clamp01(currentSpeed / 320f);
            float speedAngle = Mathf.Lerp(-125f, 125f, speedRatio);

            DrawRotatedNeedle(speedCenter, speedAngle, dialSize * 0.42f, speedNeedleTex);

            // 4. Centro del velocímetro (Tapa central con Speed digital y KM/H)
            if (dialCapTex != null)
            {
                float capSize = 72f;
                Rect capRect = new Rect(speedCenter.x - capSize * 0.5f, speedCenter.y - capSize * 0.5f, capSize, capSize);
                GUI.DrawTexture(capRect, dialCapTex);
            }

            GUI.Label(new Rect(speedCenter.x - 45f, speedCenter.y - 28f, 90f, 36f), $"{Mathf.RoundToInt(currentSpeed)}", speedStyle);
            GUI.Label(new Rect(speedCenter.x - 35f, speedCenter.y + 8f, 70f, 22f), "KM/H", rpmStyle);
        }

        /// <summary>
        /// Dibuja una aguja deportiva con rotación fluida alrededor del pivote central.
        /// </summary>
        private void DrawRotatedNeedle(Vector2 pivot, float angleDegrees, float length, Texture2D needleTex)
        {
            if (needleTex == null) return;

            Matrix4x4 backup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angleDegrees, pivot);

            float needleW = 10f;
            Rect needleRect = new Rect(pivot.x - needleW * 0.5f, pivot.y - length, needleW, length);
            GUI.DrawTexture(needleRect, needleTex);

            GUI.matrix = backup;
        }

        /// <summary>
        /// Coloca textos numéricos ordenados alrededor del perímetro del reloj circular.
        /// </summary>
        private void DrawDialLabels(Vector2 center, float radius, string[] labels, float startAngle, float endAngle)
        {
            if (labels == null || labels.Length == 0) return;

            for (int i = 0; i < labels.Length; i++)
            {
                float angleDeg = Mathf.Lerp(startAngle, endAngle, (float)i / (labels.Length - 1));
                // Convertir ángulo (0 arriba, + horario) a radianes trigonométricos
                float rad = (angleDeg - 90f) * Mathf.Deg2Rad;
                float px = center.x + Mathf.Cos(rad) * radius;
                float py = center.y + Mathf.Sin(rad) * radius;

                Rect labelRect = new Rect(px - 22f, py - 12f, 44f, 24f);
                GUI.Label(labelRect, labels[i], dialLabelStyle);
            }
        }

        /// <summary>
        /// Menú modal de pausa al presionar la tecla Escape.
        /// </summary>
        private void DrawPauseMenu(CarPhysicsController car)
        {
            // Sombra oscura en toda la pantalla
            if (dimTex != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), dimTex);
            }

            float panelW = showingSettings ? 560f : 480f;
            float panelH = showingSettings ? 460f : 370f;
            float panelX = (Screen.width - panelW) * 0.5f;
            float panelY = (Screen.height - panelH) * 0.5f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", boxStyle);

            float contentX = panelX + 25f;
            float contentW = panelW - 50f;
            float curY = panelY + 20f;

            if (!showingSettings)
            {
                GUI.Label(new Rect(contentX, curY, contentW, 45), "JUEGO EN PAUSA", pauseTitleStyle);
                curY += 60f;

                float btnH = 44f;
                float gap = 52f;

                // 1. REANUDAR
                if (GUI.Button(new Rect(contentX, curY, contentW, btnH), "▶ REANUDAR (ESC)", pauseBtnPrimaryStyle))
                {
                    TogglePause();
                }
                curY += gap;

                // 2. CONFIGURACIÓN
                if (GUI.Button(new Rect(contentX, curY, contentW, btnH), "⚙ CONFIGURACIÓN (GRÁFICOS Y SONIDO)", pauseBtnSecondaryStyle))
                {
                    showingSettings = true;
                }
                curY += gap;

                // 3. REINICIAR PISTA
                if (GUI.Button(new Rect(contentX, curY, contentW, btnH), "🔄 REINICIAR PISTA", pauseBtnSecondaryStyle))
                {
                    Time.timeScale = 1f;
                    isPaused = false;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                curY += gap;

                // 4. MENÚ PRINCIPAL
                if (GUI.Button(new Rect(contentX, curY, contentW, btnH), "🏠 MENÚ PRINCIPAL", pauseBtnSecondaryStyle))
                {
                    Time.timeScale = 1f;
                    isPaused = false;
                    SceneManager.LoadScene("MainMenu");
                }
                curY += gap;

                // 5. SALIR AL ESCRITORIO
                if (GUI.Button(new Rect(contentX, curY, contentW, 38f), "✕ SALIR AL ESCRITORIO", pauseBtnSecondaryStyle))
                {
                    Application.Quit();
                }
            }
            else
            {
                // Submenú de Configuración en Pausa con ScrollView
                float scrollH = panelH - 80f;
                Rect scrollRect = new Rect(contentX, curY, contentW, scrollH);
                Rect scrollContentRect = new Rect(0, 0, contentW - 20f, 380f);

                pauseScrollPos = GUI.BeginScrollView(scrollRect, pauseScrollPos, scrollContentRect);
                float sY = 0f;

                GUI.Label(new Rect(0, sY, scrollContentRect.width, 36), "⚙ CONFIGURACIÓN", pauseTitleStyle);
                sY += 46f;

                // Calidad Gráfica
                GUI.Label(new Rect(0, sY, scrollContentRect.width, 22), "CALIDAD GRÁFICA:", titleStyle);
                sY += 28f;

                string[] qualityNames = QualitySettings.names;
                if (qualityNames == null || qualityNames.Length == 0)
                {
                    qualityNames = new string[] { "Baja", "Media", "Alta", "Ultra" };
                }

                int currentQuality = QualitySettings.GetQualityLevel();
                float qBtnW = (scrollContentRect.width - (qualityNames.Length - 1) * 6f) / qualityNames.Length;

                for (int i = 0; i < qualityNames.Length; i++)
                {
                    GUIStyle qStyle = (i == currentQuality) ? carBtnActiveStyle : carBtnStyle;
                    if (GUI.Button(new Rect(i * (qBtnW + 6f), sY, qBtnW, 36), qualityNames[i], qStyle))
                    {
                        SettingsManager.SetQualityLevel(i);
                    }
                }
                sY += 50f;

                // Volumen de Sonido
                float vol = SettingsManager.GetMasterVolume();
                int volPercent = Mathf.RoundToInt(vol * 100f);
                GUI.Label(new Rect(0, sY, scrollContentRect.width, 22), $"VOLUMEN MAESTRO: {volPercent}%", titleStyle);
                sY += 28f;

                if (GUI.Button(new Rect(0, sY, 45, 32), " - ", carBtnStyle))
                {
                    SettingsManager.SetMasterVolume(Mathf.Max(0f, vol - 0.1f));
                }

                float newVol = GUI.HorizontalSlider(new Rect(55, sY + 8, scrollContentRect.width - 110, 20), vol, 0f, 1f);
                if (Mathf.Abs(newVol - vol) > 0.01f)
                {
                    SettingsManager.SetMasterVolume(newVol);
                }

                if (GUI.Button(new Rect(scrollContentRect.width - 45, sY, 45, 32), " + ", carBtnStyle))
                {
                    SettingsManager.SetMasterVolume(Mathf.Min(1f, vol + 0.1f));
                }
                sY += 52f;

                // Dirección del volante
                if (car != null)
                {
                    string invText = car.invertSteering ? "INVERTIDO: SÍ (Recomendado para Firefox Móvil)" : "INVERTIDO: NO";
                    GUI.Label(new Rect(0, sY, scrollContentRect.width, 22), "DIRECCIÓN DEL VOLANTE:", titleStyle);
                    sY += 28f;
                    if (GUI.Button(new Rect(0, sY, scrollContentRect.width, 36), invText, carBtnStyle))
                    {
                        car.invertSteering = !car.invertSteering;
                        SettingsManager.SetInvertSteering(car.invertSteering);
                    }
                    sY += 52f;
                }

                GUI.EndScrollView();

                // Botón Volver fijo abajo
                if (GUI.Button(new Rect(contentX + (contentW - 180) * 0.5f, panelY + panelH - 48f, 180, 38), "◀ VOLVER", pauseBtnPrimaryStyle))
                {
                    showingSettings = false;
                }
            }
        }

        private void DrawConnectionPanel(bool isConnected)
        {
            // Si el móvil ya está conectado y el usuario no forzó ver el panel grande, mostrar una pastilla compacta y limpia
            if (isConnected && !forceShowFullQr)
            {
                float miniW = 280f;
                float miniH = 38f;
                float miniX = Screen.width - miniW - 15f;
                float miniY = 14f;

                GUI.Box(new Rect(miniX, miniY, miniW, miniH), "", boxStyle);

                GUIStyle miniStatusStyle = new GUIStyle(titleStyle)
                {
                    fontSize = 12,
                    normal = { textColor = new Color(0.35f, 0.95f, 0.45f) }
                };
                GUI.Label(new Rect(miniX + 10, miniY + 10, miniW - 80, 20), "🟢 VOLANTE CONECTADO (60 FPS)", miniStatusStyle);

                if (GUI.Button(new Rect(miniX + miniW - 65, miniY + 7, 55, 24), "⚙ QR", carBtnStyle))
                {
                    forceShowFullQr = true;
                }
                return;
            }

            // Panel completo expandido con QR
            float w = 390f;
            float h = 230f;
            float x = Screen.width - w - 15f;
            float y = 14f;

            GUI.Box(new Rect(x, y, w, h), "", boxStyle);

            string statusText = isConnected ? "🟢 VOLANTE CONECTADO (60 FPS)" : "🔴 ESPERANDO CONEXIÓN MÓVIL";
            Color statusColor = isConnected ? new Color(0.35f, 0.95f, 0.45f) : new Color(1f, 0.4f, 0.4f);

            GUIStyle st = new GUIStyle(titleStyle) { fontSize = 13, normal = { textColor = statusColor } };
            GUI.Label(new Rect(x + 12, y + 8, w - 90, 20), statusText, st);

            if (isConnected)
            {
                if (GUI.Button(new Rect(x + w - 75, y + 6, 65, 22), "▲ OCULTAR", carBtnStyle))
                {
                    forceShowFullQr = false;
                }
            }

            string url = TelemetryServer.Instance != null ? TelemetryServer.Instance.ServerUrl : "http://localhost:8090/";
            GUI.Label(new Rect(x + 12, y + 30, 200, 22), $"{url}", rpmStyle);

            // Botones de control de red
            int ipCount = TelemetryServer.Instance != null ? TelemetryServer.Instance.LocalIPs.Count : 1;
            int ipIdx = TelemetryServer.Instance != null ? (TelemetryServer.Instance.SelectedIPIndex + 1) : 1;

            if (GUI.Button(new Rect(x + 225, y + 28, 75, 24), $"IP ({ipIdx}/{ipCount})", carBtnStyle))
            {
                TelemetryServer.Instance?.CycleNextIP();
            }

            if (GUI.Button(new Rect(x + 305, y + 28, 70, 24), "Escanear", carBtnStyle))
            {
                TelemetryServer.Instance?.FindLocalIPAddresses();
            }

            // Mostrar código QR
            if (qrTexture != null)
            {
                GUI.DrawTexture(new Rect(x + 12, y + 55, 120, 120), qrTexture);
            }

            // Instrucciones rápidas con wordWrap
            string instructions = "1. Conecta por USB o Wi-Fi.\n2. Abre Firefox en tu celular.\n3. Entra a la IP mostrada arriba o escanea este QR.";
            GUIStyle instStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.92f, 0.96f) }
            };
            GUI.Label(new Rect(x + 140, y + 55, 235, 120), instructions, instStyle);

            // Log de telemetría en tiempo real
            string logText = TelemetryServer.Instance != null ? TelemetryServer.Instance.LastActivityLog : "";
            GUIStyle logSt = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = new Color(0.5f, 0.9f, 1.0f) } };
            GUI.Label(new Rect(x + 12, y + 190, w - 24, 24), $"> {logText}", logSt);
        }

        private void DrawCarSelector()
        {
            var cars = CarSelectionManager.Instance != null ? CarSelectionManager.Instance.cars : null;
            int carCount = (cars != null && cars.Length > 0) ? cars.Length : 0;
            if (carCount == 0) return;

            float w = 265f;
            float h = 48f + (carCount * 36f);
            float x = 15f;
            float y = 15f;

            GUI.Box(new Rect(x, y, w, h), "", boxStyle);
            GUI.Label(new Rect(x + 12, y + 8, w - 24, 22), "SELECCIÓN DE AUTO", titleStyle);

            int current = CarSelectionManager.Instance != null ? CarSelectionManager.Instance.currentCarIndex : 0;
            for (int i = 0; i < cars.Length; i++)
            {
                if (cars[i] == null) continue;
                bool isSelected = (i == current);
                GUIStyle btnSt = isSelected ? carBtnActiveStyle : carBtnStyle;

                Rect btnRect = new Rect(x + 12, y + 36 + (i * 36), w - 24, 32);
                string carDisplayName = !string.IsNullOrEmpty(cars[i].vehicleName) ? cars[i].vehicleName : cars[i].name;
                if (GUI.Button(btnRect, $"{i + 1}. {carDisplayName} ({Mathf.RoundToInt(cars[i].engineHorsepower)}HP)", btnSt))
                {
                    CarSelectionManager.Instance?.SelectCar(i);
                }
            }
        }

        private void DrawLapTimer()
        {
            if (LapTimer.Instance == null) return;

            float w = 320f;
            float h = 68f;
            float x = (Screen.width - w) * 0.5f;
            float y = 14f;

            GUI.Box(new Rect(x, y, w, h), "", boxStyle);

            string curLap = LapTimer.FormatTime(LapTimer.Instance.CurrentLapTime);
            string bestLap = LapTimer.FormatTime(LapTimer.Instance.BestLapTime);
            int lapNum = LapTimer.Instance.CurrentLap;

            GUI.Label(new Rect(x + 10, y + 8, w - 20, 24), $"⏱️ VUELTA {lapNum}: {curLap}", titleStyle);
            GUI.Label(new Rect(x + 10, y + 36, w - 20, 20), $"★ RÉCORD PISTA: {bestLap}", rpmStyle);
        }

        /// <summary>
        /// Genera texturas circulares procedimentales con bisel, marcas de graduación y zona roja.
        /// </summary>
        private Texture2D CreateDialTexture(int size, bool isTachometer)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float half = size * 0.5f;
            Color transparent = new Color(0, 0, 0, 0);
            Color bezelOuter = new Color(0.28f, 0.32f, 0.38f, 0.95f);
            Color bezelInner = new Color(0.12f, 0.15f, 0.20f, 0.95f);
            Color faceCenter = new Color(0.08f, 0.11f, 0.16f, 0.92f);
            Color faceRim = new Color(0.04f, 0.05f, 0.08f, 0.95f);
            Color redlineColor = new Color(0.95f, 0.18f, 0.18f, 0.90f);
            Color tickWhite = new Color(0.95f, 0.95f, 0.95f, 0.95f);
            Color tickSubtle = new Color(0.55f, 0.60f, 0.68f, 0.75f);

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 1.0f)
                    {
                        pixels[y * size + x] = transparent;
                        continue;
                    }

                    if (dist > 0.97f)
                    {
                        float alpha = (1.0f - dist) / 0.03f;
                        pixels[y * size + x] = new Color(bezelOuter.r, bezelOuter.g, bezelOuter.b, bezelOuter.a * alpha);
                        continue;
                    }

                    if (dist > 0.91f)
                    {
                        float t = (dist - 0.91f) / 0.06f;
                        pixels[y * size + x] = Color.Lerp(bezelInner, bezelOuter, t);
                        continue;
                    }

                    float faceT = dist / 0.91f;
                    Color col = Color.Lerp(faceCenter, faceRim, faceT);

                    float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;

                    if (angle >= -132f && angle <= 132f)
                    {
                        if (isTachometer)
                        {
                            if (angle > 78f && dist >= 0.81f && dist <= 0.89f)
                            {
                                col = redlineColor;
                            }

                            for (int i = 0; i <= 8; i++)
                            {
                                float tickAngle = Mathf.Lerp(-125f, 125f, i / 8.5f);
                                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle, tickAngle));
                                if (angleDiff < 1.1f && dist >= 0.72f && dist <= 0.88f)
                                {
                                    col = (i >= 7) ? redlineColor : tickWhite;
                                }
                                else if (i < 8)
                                {
                                    float midAngle = Mathf.Lerp(-125f, 125f, (i + 0.5f) / 8.5f);
                                    float midDiff = Mathf.Abs(Mathf.DeltaAngle(angle, midAngle));
                                    if (midDiff < 0.8f && dist >= 0.79f && dist <= 0.88f)
                                    {
                                        col = tickSubtle;
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i <= 10; i++)
                            {
                                float tickAngle = Mathf.Lerp(-125f, 125f, i / 10f);
                                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle, tickAngle));
                                if (angleDiff < 1.1f && dist >= 0.72f && dist <= 0.88f)
                                {
                                    col = tickWhite;
                                }
                                else if (i < 10)
                                {
                                    float midAngle = Mathf.Lerp(-125f, 125f, (i + 0.5f) / 10f);
                                    float midDiff = Mathf.Abs(Mathf.DeltaAngle(angle, midAngle));
                                    if (midDiff < 0.8f && dist >= 0.79f && dist <= 0.88f)
                                    {
                                        col = tickSubtle;
                                    }
                                }
                            }
                        }
                    }

                    if (dist < 0.32f)
                    {
                        if (dist > 0.30f) col = bezelOuter;
                        else col = new Color(0.05f, 0.07f, 0.10f, 0.98f);
                    }

                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Genera la textura de la aguja deportiva afilada con brillo central.
        /// </summary>
        private Texture2D CreateNeedleTexture(int width, int height, Color needleColor)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pix = new Color[width * height];
            Color transparent = new Color(0, 0, 0, 0);

            float halfW = width * 0.5f;

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                float currentW = Mathf.Lerp(halfW, 1.2f, t);

                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Abs(x - halfW);
                    if (dx <= currentW)
                    {
                        float intensity = 1.0f - (dx / currentW) * 0.3f;
                        pix[y * width + x] = new Color(needleColor.r * intensity, needleColor.g * intensity, needleColor.b * intensity, needleColor.a);
                    }
                    else
                    {
                        pix[y * width + x] = transparent;
                    }
                }
            }

            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Genera la tapa central oscura del eje del reloj circular.
        /// </summary>
        private Texture2D CreateCapTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pix = new Color[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 1.0f)
                    {
                        pix[y * size + x] = Color.clear;
                    }
                    else if (dist > 0.90f)
                    {
                        pix[y * size + x] = new Color(0.35f, 0.40f, 0.48f, 0.95f); // Aro exterior del tapón
                    }
                    else
                    {
                        float t = dist / 0.90f;
                        pix[y * size + x] = Color.Lerp(new Color(0.12f, 0.15f, 0.20f, 0.98f), new Color(0.04f, 0.05f, 0.07f, 0.98f), t);
                    }
                }
            }

            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
