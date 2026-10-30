# 🏎️ GyroPilot 360 - Simulador de Automovilismo con Marchas y Volante Telemétrico Móvil

> **Simulador de automovilismo de competición en primera persona desarrollado en Unity 6.**  
> Permite convertir cualquier smartphone en un volante telemétrico inalámbrico a 60 FPS con giroscopio, pedales hápticos y palanca de cambios manual en H sin necesidad de periféricos costosos.

---

## 👥 Equipo y Créditos del Proyecto

### 🏆 Desarrolladores Principales
* **Juan Pablo López** - *Desarrollador Principal & Arquitectura del Simulador*
* **Antigravity (AI Coding Assistant - Google DeepMind)** - *Co-Desarrollador & Asistencia Avanzada de Ingeniería*

### 🏎️ Colaboradores
* **Oscar Mauricio Sandoval Márquez**
* **Joseph Sneijder Delgado Tarazona**
* **Kevin Andrés Correa Martinez**

### 🏫 Institución y Asesoría Docente
* **Institución Educativa**: Colegio Carlos Vicente Rey
* **Docente Asesor**: Profesor Ronald Barrera

---

## ⚡ Características Principales

* **Volante Telemétrico Inalámbrico (Zero-Install)**: No requiere instalar ninguna aplicación en el móvil. Funciona como una Web App sobre el navegador **Mozilla Firefox Mobile** comunicándose mediante WebSockets a 60 FPS (~1-2 ms de latencia por USB).
* **Transmisión Manual en H con Flatshift**: Caja de 5 velocidades más reversa. Permite cambios sin soltar el acelerador gracias a un corte de inyección de competición (*Flatshift* de 150 ms).
* **Freno de Motor Dinámico y Rev Limiter**: Retención de compresión dinámica proporcional a las RPM y a la marcha engranada (hasta 6.800 N en reducciones a 1ª/2ª marcha) y corte estricto de revoluciones (*Rev Limiter* deportivo con rebote).
* **Cuadro de Mandos Análogo Procedural**: Tacómetro y velocímetro circulares generados matemáticamente en tiempo real con aguja deportiva y barras de telemetría dinámica de presión de pedales (Gas y Freno).
* **Modos de Juego**:
  * 🏁 **Modo Circuito GP**: Pista cerrada de asfalto con pianos, curvas técnicas y sistema de cronometraje por sensor de paso (Vuelta actual y récord).
  * ⚡ **Modo Recta Infinita 300+ km/h**: Autopista procedural infinita con postes de luz, radares de velocidad, guardarraíles y obstáculos aleatorios para pruebas de velocidad punta.
* **Menú Principal y Pausa**: Ajustes en vivo de calidad gráfica, volumen maestro, inversión de volante y pantalla de créditos institucionales.

---

## 📱 Guía Rápida de Conexión del Volante Móvil

### 1. Requisito Fundamental: Mozilla Firefox
Por directiva de seguridad del consorcio W3C (*Secure Contexts*), los navegadores basados en Chromium (Chrome, Edge) bloquean el giroscopio en redes locales HTTP (`http://192.168.x.x`). **Mozilla Firefox Mobile** respeta el estándar `DeviceOrientation` en redes locales privadas, permitiendo la lectura de los sensores sin necesidad de certificados SSL.

### 2. Conexión Paso a Paso:
1. **Conexión Recomendada (Cable USB)**:
   * Conecta tu teléfono al PC mediante cable USB.
   * En tu teléfono, activa **Anclaje a red USB** (*USB Tethering*) en los ajustes de red.
   *(También es compatible vía Wi-Fi conectando ambos equipos a la misma red local).*
2. **Ejecutar el Simulador**:
   * Abre el proyecto en Unity 6 (`6000.3.2f1`) y abre la escena `Assets/Scenes/MainMenu.unity`.
   * Presiona **Play (▶)**.
3. **Abrir la Interfaz en Firefox**:
   * Abre Firefox en tu celular e ingresa la dirección IP que aparece en pantalla (ej: `http://192.168.42.129:8090`) o escanea el código QR desplegado en el juego.
   * Concede los permisos de orientación cuando el navegador lo solicite.
4. **Conducción**:
   * Sujeta el teléfono en horizontal con ambas manos.
   * Presiona **🎯 CALIBRAR CENTRO** para fijar tu posición neutra.
   * Enciende el motor con **⚡ ENCENDER AUTO** (o tecla `E` en PC).
   * Acelera con el pulgar izquierdo y selecciona marchas en la palanca en H con el pulgar derecho.

---

## 🛠️ Requisitos del Sistema
* **Motor**: Unity 6000.3.2f1 o superior.
* **Plataforma**: Windows (PC Standalone / Editor).
* **Móvil**: Cualquier smartphone con giroscopio y Mozilla Firefox instalado.
