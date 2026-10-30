using System;
using System.Collections.Generic;
using UnityEngine;
using GyroPilot.Vehicle;

namespace GyroPilot.Track
{
    public class InfiniteRoadManager : MonoBehaviour
    {
        public static InfiniteRoadManager Instance { get; private set; }

        [Header("Configuración de la Recta")]
        public float segmentLength = 100f;
        public int totalSegments = 12; // 1200 metros de carretera visibles continuamente
        public float roadWidth = 22f;  // 4 carriles (5m c/u) + arcenes
        public Transform vehicleSpawnPoint;

        [Header("Telemetría de la Recta")]
        public float currentDistanceMeters = 0f;
        public float sessionTopSpeedKmh = 0f;
        public float allTimeRecordKmh = 0f;
        public int obstaclesDodged = 0;

        private const string PREF_TOP_SPEED = "GyroPilot_InfiniteTopSpeed";
        private CarPhysicsController targetCar;
        private List<RoadSegment> segments = new List<RoadSegment>();

        // Materiales procedimentales
        private Material asphaltMat;
        private Material roadLineMat;
        private Material shoulderLineMat;
        private Material guardrailMat;
        private Material poleMat;
        private Material lightGlowMat;
        private Material gantryMat;
        private Material neonSignMat;
        private Material coneOrangeMat;
        private Material coneWhiteMat;
        private Material crateMat;
        private Material billboardMat;

        private class RoadSegment
        {
            public GameObject root;
            public float startZ;
            public List<GameObject> obstacles = new List<GameObject>();
            public GameObject gantrySign;
            public TextMesh gantryText;
        }

        private void Awake()
        {
            Instance = this;
            allTimeRecordKmh = PlayerPrefs.GetFloat(PREF_TOP_SPEED, 0f);
            CreateProceduralMaterials();
        }

        private void Start()
        {
            // Crear el punto de spawn si no existe
            if (vehicleSpawnPoint == null)
            {
                GameObject spawnObj = new GameObject("[SPAWN_VEHICULO]");
                spawnObj.transform.position = new Vector3(0f, 0.4f, 10f);
                spawnObj.transform.rotation = Quaternion.identity;
                vehicleSpawnPoint = spawnObj.transform;
            }

            BuildInitialHighway();
        }

        private void CreateProceduralMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Asfalto de alta adherencia
            asphaltMat = new Material(litShader);
            asphaltMat.color = new Color(0.12f, 0.13f, 0.16f);
            if (asphaltMat.HasProperty("_Smoothness")) asphaltMat.SetFloat("_Smoothness", 0.25f);

            // 2. Líneas discontinuas blancas
            roadLineMat = new Material(litShader);
            roadLineMat.color = new Color(0.95f, 0.95f, 0.95f);
            if (roadLineMat.HasProperty("_Smoothness")) roadLineMat.SetFloat("_Smoothness", 0.6f);

            // 3. Líneas amarillas de arcén
            shoulderLineMat = new Material(litShader);
            shoulderLineMat.color = new Color(1.0f, 0.85f, 0.15f);

            // 4. Guardarraíl metálico galvanizado
            guardrailMat = new Material(litShader);
            guardrailMat.color = new Color(0.65f, 0.70f, 0.78f);
            if (guardrailMat.HasProperty("_Metallic")) guardrailMat.SetFloat("_Metallic", 0.85f);
            if (guardrailMat.HasProperty("_Smoothness")) guardrailMat.SetFloat("_Smoothness", 0.75f);

            // 5. Postes de luz oscuros
            poleMat = new Material(litShader);
            poleMat.color = new Color(0.20f, 0.22f, 0.26f);
            if (poleMat.HasProperty("_Metallic")) poleMat.SetFloat("_Metallic", 0.7f);

            // 6. Luz de farola emisiva
            lightGlowMat = new Material(litShader);
            lightGlowMat.color = new Color(1.0f, 0.95f, 0.8f);
            if (lightGlowMat.HasProperty("_EmissionColor"))
            {
                lightGlowMat.EnableKeyword("_EMISSION");
                lightGlowMat.SetColor("_EmissionColor", new Color(1.0f, 0.95f, 0.75f) * 2.5f);
            }

            // 7. Pórtico de velocidad (Gantry metálico)
            gantryMat = new Material(litShader);
            gantryMat.color = new Color(0.18f, 0.22f, 0.30f);

            // 8. Pantalla LED de neón en el pórtico
            neonSignMat = new Material(litShader);
            neonSignMat.color = new Color(0.1f, 0.85f, 1.0f);
            if (neonSignMat.HasProperty("_EmissionColor"))
            {
                neonSignMat.EnableKeyword("_EMISSION");
                neonSignMat.SetColor("_EmissionColor", new Color(0.1f, 0.85f, 1.0f) * 3.0f);
            }

            // 9. Cono Naranja
            coneOrangeMat = new Material(litShader);
            coneOrangeMat.color = new Color(1.0f, 0.35f, 0.05f);

            // 10. Cono Blanco
            coneWhiteMat = new Material(litShader);
            coneWhiteMat.color = Color.white;

            // 11. Cajas de madera / obstáculos
            crateMat = new Material(litShader);
            crateMat.color = new Color(0.65f, 0.45f, 0.25f);

            // 12. Vallas publicitarias
            billboardMat = new Material(litShader);
            billboardMat.color = new Color(0.15f, 0.25f, 0.45f);
            if (billboardMat.HasProperty("_EmissionColor"))
            {
                billboardMat.EnableKeyword("_EMISSION");
                billboardMat.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.8f) * 1.5f);
            }
        }

        private void BuildInitialHighway()
        {
            for (int i = 0; i < totalSegments; i++)
            {
                float z = i * segmentLength;
                RoadSegment seg = CreateSegment(z, i);
                segments.Add(seg);
            }
        }

        private RoadSegment CreateSegment(float z, int index)
        {
            RoadSegment seg = new RoadSegment();
            seg.startZ = z;
            seg.root = new GameObject($"Segment_{index:D2}_{z:F0}m");
            seg.root.transform.position = new Vector3(0f, 0f, z);
            seg.root.transform.SetParent(transform);

            // 1. Asfalto de la autopista
            GameObject roadQuad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadQuad.name = "Asphalt";
            roadQuad.transform.SetParent(seg.root.transform);
            roadQuad.transform.localPosition = new Vector3(0f, -0.1f, segmentLength * 0.5f);
            roadQuad.transform.localScale = new Vector3(roadWidth, 0.2f, segmentLength);
            roadQuad.GetComponent<Renderer>().material = asphaltMat;

            // 2. Terreno lateral oscuro (Llanura)
            GameObject leftGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftGround.name = "LeftShoulder";
            leftGround.transform.SetParent(seg.root.transform);
            leftGround.transform.localPosition = new Vector3(-roadWidth * 0.5f - 40f, -0.15f, segmentLength * 0.5f);
            leftGround.transform.localScale = new Vector3(80f, 0.15f, segmentLength);
            leftGround.GetComponent<Renderer>().material = asphaltMat;
            Destroy(leftGround.GetComponent<Collider>());

            GameObject rightGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightGround.name = "RightShoulder";
            rightGround.transform.SetParent(seg.root.transform);
            rightGround.transform.localPosition = new Vector3(roadWidth * 0.5f + 40f, -0.15f, segmentLength * 0.5f);
            rightGround.transform.localScale = new Vector3(80f, 0.15f, segmentLength);
            rightGround.GetComponent<Renderer>().material = asphaltMat;
            Destroy(rightGround.GetComponent<Collider>());

            // 3. Líneas amarillas de borde continuo
            CreateRoadLine(seg.root.transform, -roadWidth * 0.5f + 0.3f, segmentLength * 0.5f, segmentLength, shoulderLineMat, isDashed: false);
            CreateRoadLine(seg.root.transform, roadWidth * 0.5f - 0.3f, segmentLength * 0.5f, segmentLength, shoulderLineMat, isDashed: false);

            // 4. Líneas divisorias de carriles (3 líneas discontinuas blancas)
            float laneWidth = roadWidth / 4f; // 5.5 metros por carril
            CreateRoadLine(seg.root.transform, -laneWidth, segmentLength * 0.5f, segmentLength, roadLineMat, isDashed: true);
            CreateRoadLine(seg.root.transform, 0f, segmentLength * 0.5f, segmentLength, roadLineMat, isDashed: true);
            CreateRoadLine(seg.root.transform, laneWidth, segmentLength * 0.5f, segmentLength, roadLineMat, isDashed: true);

            // 5. Guardarraíles de seguridad en ambos bordes
            CreateGuardrail(seg.root.transform, -roadWidth * 0.5f, segmentLength);
            CreateGuardrail(seg.root.transform, roadWidth * 0.5f, segmentLength);

            // 6. Farolas de autopista (cada 50m en ambos lados)
            CreateStreetLight(seg.root.transform, -roadWidth * 0.5f - 1.2f, 25f, isLeft: true);
            CreateStreetLight(seg.root.transform, roadWidth * 0.5f + 1.2f, 25f, isLeft: false);
            CreateStreetLight(seg.root.transform, -roadWidth * 0.5f - 1.2f, 75f, isLeft: true);
            CreateStreetLight(seg.root.transform, roadWidth * 0.5f + 1.2f, 75f, isLeft: false);

            // 7. Pórtico de Radar LED cada 300 metros
            if (Mathf.RoundToInt(z) % 300 == 0 && z > 50f)
            {
                CreateSpeedRadarGantry(seg);
            }

            // 8. Valla publicitaria en el lateral cada 150m
            if (Mathf.RoundToInt(z) % 150 == 0)
            {
                float sideX = (index % 2 == 0) ? -roadWidth * 0.5f - 8f : roadWidth * 0.5f + 8f;
                CreateBillboard(seg.root.transform, sideX, 50f);
            }

            // 9. Generar obstáculos dinámicos en este segmento
            if (z > 80f) // No colocar obstáculos en la salida inmediata
            {
                SpawnSegmentObstacles(seg);
            }

            return seg;
        }

        private void CreateRoadLine(Transform parent, float x, float zCenter, float length, Material mat, bool isDashed)
        {
            if (!isDashed)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "SolidLine";
                line.transform.SetParent(parent);
                line.transform.localPosition = new Vector3(x, 0.015f, zCenter);
                line.transform.localScale = new Vector3(0.20f, 0.01f, length);
                line.GetComponent<Renderer>().material = mat;
                Destroy(line.GetComponent<Collider>());
            }
            else
            {
                // Líneas discontinuas (tramos de 4m con 6m de espacio)
                float step = 10f;
                int count = Mathf.FloorToInt(length / step);
                for (int i = 0; i < count; i++)
                {
                    GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    dash.name = "DashedLine";
                    dash.transform.SetParent(parent);
                    dash.transform.localPosition = new Vector3(x, 0.015f, (i * step) + 3f);
                    dash.transform.localScale = new Vector3(0.18f, 0.01f, 4.5f);
                    dash.GetComponent<Renderer>().material = mat;
                    Destroy(dash.GetComponent<Collider>());
                }
            }
        }

        private void CreateGuardrail(Transform parent, float x, float length)
        {
            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Guardrail";
            rail.transform.SetParent(parent);
            rail.transform.localPosition = new Vector3(x, 0.45f, length * 0.5f);
            rail.transform.localScale = new Vector3(0.25f, 0.65f, length);
            rail.GetComponent<Renderer>().material = guardrailMat;

            // Reflectores rojos y blancos en el guardarraíl cada 15m
            for (float rZ = 5f; rZ < length; rZ += 15f)
            {
                GameObject reflector = GameObject.CreatePrimitive(PrimitiveType.Cube);
                reflector.name = "Reflector";
                reflector.transform.SetParent(rail.transform);
                float inOffset = (x > 0) ? -0.14f : 0.14f;
                reflector.transform.localPosition = new Vector3(inOffset, 0.15f, (rZ / length) - 0.5f);
                reflector.transform.localScale = new Vector3(0.08f, 0.25f, 0.8f / length);
                reflector.GetComponent<Renderer>().material = neonSignMat;
                Destroy(reflector.GetComponent<Collider>());
            }
        }

        private void CreateStreetLight(Transform parent, float x, float localZ, bool isLeft)
        {
            GameObject poleRoot = new GameObject("StreetLight");
            poleRoot.transform.SetParent(parent);
            poleRoot.transform.localPosition = new Vector3(x, 0f, localZ);

            // Poste vertical alto
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.transform.SetParent(poleRoot.transform);
            post.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            post.transform.localScale = new Vector3(0.25f, 4.5f, 0.25f);
            post.GetComponent<Renderer>().material = poleMat;
            Destroy(post.GetComponent<Collider>());

            // Brazo horizontal arqueado hacia la carretera
            float armDir = isLeft ? 1f : -1f;
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.transform.SetParent(poleRoot.transform);
            arm.transform.localPosition = new Vector3(armDir * 1.8f, 8.8f, 0f);
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, armDir * -75f);
            arm.transform.localScale = new Vector3(0.18f, 2.0f, 0.18f);
            arm.GetComponent<Renderer>().material = poleMat;
            Destroy(arm.GetComponent<Collider>());

            // Lámpara emisiva
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lamp.transform.SetParent(poleRoot.transform);
            lamp.transform.localPosition = new Vector3(armDir * 3.5f, 8.5f, 0f);
            lamp.transform.localScale = new Vector3(0.9f, 0.25f, 1.4f);
            lamp.GetComponent<Renderer>().material = lightGlowMat;
            Destroy(lamp.GetComponent<Collider>());
        }

        private void CreateSpeedRadarGantry(RoadSegment seg)
        {
            GameObject gantryRoot = new GameObject("SpeedRadarGantry");
            gantryRoot.transform.SetParent(seg.root.transform);
            gantryRoot.transform.localPosition = new Vector3(0f, 0f, 50f);

            // Pilar izquierdo
            GameObject pLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pLeft.transform.SetParent(gantryRoot.transform);
            pLeft.transform.localPosition = new Vector3(-roadWidth * 0.5f - 0.8f, 4.5f, 0f);
            pLeft.transform.localScale = new Vector3(0.8f, 9.0f, 0.8f);
            pLeft.GetComponent<Renderer>().material = gantryMat;

            // Pilar derecho
            GameObject pRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pRight.transform.SetParent(gantryRoot.transform);
            pRight.transform.localPosition = new Vector3(roadWidth * 0.5f + 0.8f, 4.5f, 0f);
            pRight.transform.localScale = new Vector3(0.8f, 9.0f, 0.8f);
            pRight.GetComponent<Renderer>().material = gantryMat;

            // Viga horizontal superior
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.transform.SetParent(gantryRoot.transform);
            beam.transform.localPosition = new Vector3(0f, 8.8f, 0f);
            beam.transform.localScale = new Vector3(roadWidth + 2.5f, 1.2f, 1.2f);
            beam.GetComponent<Renderer>().material = gantryMat;

            // Pantalla LED central de radar
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.transform.SetParent(gantryRoot.transform);
            screen.transform.localPosition = new Vector3(0f, 8.6f, -0.65f);
            screen.transform.localScale = new Vector3(10f, 1.5f, 0.2f);
            screen.GetComponent<Renderer>().material = neonSignMat;
            Destroy(screen.GetComponent<Collider>());

            // Texto 3D del radar
            GameObject textObj = new GameObject("RadarTextMesh");
            textObj.transform.SetParent(screen.transform);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            textObj.transform.localRotation = Quaternion.identity;

            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = "RADAR 300 KM/H";
            tm.fontSize = 32;
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            seg.gantrySign = gantryRoot;
            seg.gantryText = tm;
        }

        private void CreateBillboard(Transform parent, float x, float localZ)
        {
            GameObject bb = new GameObject("HighwayBillboard");
            bb.transform.SetParent(parent);
            bb.transform.localPosition = new Vector3(x, 0f, localZ);

            // Soporte
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.transform.SetParent(bb.transform);
            post.transform.localPosition = new Vector3(0f, 5.0f, 0f);
            post.transform.localScale = new Vector3(0.5f, 5.0f, 0.5f);
            post.GetComponent<Renderer>().material = poleMat;
            Destroy(post.GetComponent<Collider>());

            // Panel publicitario
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.transform.SetParent(bb.transform);
            panel.transform.localPosition = new Vector3(0f, 9.5f, 0f);
            panel.transform.localScale = new Vector3(12.0f, 4.5f, 0.4f);
            panel.GetComponent<Renderer>().material = billboardMat;
            Destroy(panel.GetComponent<Collider>());

            // Letrero publicitario
            GameObject textObj = new GameObject("BillboardText");
            textObj.transform.SetParent(panel.transform);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            TextMesh tm = textObj.AddComponent<TextMesh>();
            string[] sponsors = { "GYROPILOT 360", "APEX TIRES", "V8 TURBO", "OCTANE 100", "NITRO SPEED" };
            tm.text = sponsors[UnityEngine.Random.Range(0, sponsors.Length)];
            tm.fontSize = 28;
            tm.characterSize = 0.15f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
        }

        private void SpawnSegmentObstacles(RoadSegment seg)
        {
            // Limpiar obstáculos previos de este segmento
            foreach (var obs in seg.obstacles)
            {
                if (obs != null) Destroy(obs);
            }
            seg.obstacles.Clear();

            float[] laneX = { -7.5f, -2.5f, 2.5f, 7.5f }; // Coordenadas X de los 4 carriles

            // Generar 1 o 2 grupos de obstáculos por segmento (a Z=35m y Z=75m)
            float[] obstacleZs = { 35f, 75f };

            foreach (float obsZ in obstacleZs)
            {
                // Dejar siempre 2 o 3 carriles libres para que sea esquivable a 300 km/h
                int blockedLane = UnityEngine.Random.Range(0, laneX.Length);
                float posX = laneX[blockedLane];

                int obstacleType = UnityEngine.Random.Range(0, 3);
                GameObject obstacleObj = null;

                switch (obstacleType)
                {
                    case 0:
                        // Grupo de 3 Conos de Tráfico con física dinámica
                        obstacleObj = CreateConeCluster(posX, obsZ, seg.root.transform);
                        break;
                    case 1:
                        // Barrera de Cajas / Barril de choque
                        obstacleObj = CreateCrateBarrier(posX, obsZ, seg.root.transform);
                        break;
                    case 2:
                        // Chicane de 2 conos espaciados
                        obstacleObj = CreateChicaneCones(posX, obsZ, seg.root.transform);
                        break;
                }

                if (obstacleObj != null)
                {
                    seg.obstacles.Add(obstacleObj);
                }
            }
        }

        private GameObject CreateConeCluster(float x, float z, Transform parent)
        {
            GameObject cluster = new GameObject("ConeCluster");
            cluster.transform.SetParent(parent);
            cluster.transform.localPosition = new Vector3(x, 0f, z);

            for (int i = 0; i < 3; i++)
            {
                float offsetX = (i - 1) * 0.9f;
                GameObject cone = CreateProceduralCone();
                cone.transform.SetParent(cluster.transform);
                cone.transform.localPosition = new Vector3(offsetX, 0f, (i % 2) * 0.5f);

                // Física activa para que vuelen con el impacto
                Rigidbody rb = cone.AddComponent<Rigidbody>();
                rb.mass = 4f;
                rb.linearDamping = 0.4f;
                rb.angularDamping = 0.5f;
            }

            return cluster;
        }

        private GameObject CreateCrateBarrier(float x, float z, Transform parent)
        {
            GameObject barrier = new GameObject("CrateBarrier");
            barrier.transform.SetParent(parent);
            barrier.transform.localPosition = new Vector3(x, 0.45f, z);

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "CrashBox";
            box.transform.SetParent(barrier.transform);
            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = new Vector3(2.2f, 0.9f, 0.9f);
            box.GetComponent<Renderer>().material = crateMat;

            // Franjas reflectantes amarillas y negras en el obstáculo
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.SetParent(box.transform);
            stripe.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            stripe.transform.localScale = new Vector3(0.9f, 0.4f, 0.05f);
            stripe.GetComponent<Renderer>().material = shoulderLineMat;
            Destroy(stripe.GetComponent<Collider>());

            Rigidbody rb = box.AddComponent<Rigidbody>();
            rb.mass = 25f;

            return barrier;
        }

        private GameObject CreateChicaneCones(float x, float z, Transform parent)
        {
            GameObject chicane = new GameObject("ChicaneCones");
            chicane.transform.SetParent(parent);
            chicane.transform.localPosition = new Vector3(x, 0f, z);

            for (int i = 0; i < 2; i++)
            {
                GameObject cone = CreateProceduralCone();
                cone.transform.SetParent(chicane.transform);
                cone.transform.localPosition = new Vector3(i * 1.5f, 0f, i * 2.0f);
                Rigidbody rb = cone.AddComponent<Rigidbody>();
                rb.mass = 4f;
            }

            return chicane;
        }

        private GameObject CreateProceduralCone()
        {
            GameObject cone = new GameObject("TrafficCone");

            // Base cuadrada negra
            GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePlate.transform.SetParent(cone.transform);
            basePlate.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            basePlate.transform.localScale = new Vector3(0.6f, 0.06f, 0.6f);
            basePlate.GetComponent<Renderer>().material = poleMat;
            Destroy(basePlate.GetComponent<Collider>());

            // Cuerpo del cono
            GameObject coneBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coneBody.transform.SetParent(cone.transform);
            coneBody.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            coneBody.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            coneBody.GetComponent<Renderer>().material = coneOrangeMat;

            // Franja blanca reflectante en el cono
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stripe.transform.SetParent(cone.transform);
            stripe.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            stripe.transform.localScale = new Vector3(0.36f, 0.12f, 0.36f);
            stripe.GetComponent<Renderer>().material = coneWhiteMat;
            Destroy(stripe.GetComponent<Collider>());

            // Collider principal
            BoxCollider col = cone.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(0.6f, 0.9f, 0.6f);

            return cone;
        }

        private void Update()
        {
            FindTargetCar();

            if (targetCar == null) return;

            float carZ = targetCar.transform.position.z;
            currentDistanceMeters = Mathf.Max(0f, carZ);

            // Registro de velocidad máxima
            float currentSpeed = Mathf.Abs(targetCar.currentSpeedKmh);
            if (currentSpeed > sessionTopSpeedKmh)
            {
                sessionTopSpeedKmh = currentSpeed;
                if (sessionTopSpeedKmh > allTimeRecordKmh)
                {
                    allTimeRecordKmh = sessionTopSpeedKmh;
                    PlayerPrefs.SetFloat(PREF_TOP_SPEED, allTimeRecordKmh);
                    PlayerPrefs.Save();
                }
            }

            // Actualizar letreros de radar de los pórticos cercanos
            foreach (var seg in segments)
            {
                if (seg.gantryText != null)
                {
                    float distToGantry = (seg.startZ + 50f) - carZ;
                    if (distToGantry > 0f && distToGantry < 150f)
                    {
                        seg.gantryText.text = $"RADAR: {Mathf.RoundToInt(currentSpeed)} KM/H";
                    }
                }
            }

            // Reciclaje infinito de tramos de autopista
            RecycleSegments(carZ);
        }

        private void RecycleSegments(float carZ)
        {
            if (segments.Count == 0) return;

            // Si el auto ha avanzado más allá del primer tramo
            RoadSegment firstSeg = segments[0];
            if (carZ > firstSeg.startZ + segmentLength + 40f)
            {
                // Quitar el primer segmento de la lista
                segments.RemoveAt(0);

                // Reposicionarlo en el frente
                RoadSegment lastSeg = segments[segments.Count - 1];
                float newZ = lastSeg.startZ + segmentLength;

                firstSeg.startZ = newZ;
                firstSeg.root.transform.position = new Vector3(0f, 0f, newZ);
                firstSeg.root.name = $"Segment_{newZ:F0}m";

                // Regenerar nuevos obstáculos aleatorios en el tramo
                SpawnSegmentObstacles(firstSeg);

                // Agregar al final
                segments.Add(firstSeg);
                obstaclesDodged += 2;
            }
        }

        private void FindTargetCar()
        {
            if (targetCar != null) return;

            // Intentar encontrar a través del CarSelectionManager
            if (CarSelectionManager.Instance != null && CarSelectionManager.Instance.ActiveCar != null)
            {
                targetCar = CarSelectionManager.Instance.ActiveCar;
                return;
            }

            // Búsqueda en escena
            targetCar = FindFirstObjectByType<CarPhysicsController>();
        }

        private Texture2D darkBoxTex;
        private Texture2D cardHeaderTex;
        private Texture2D dividerTex;
        private Texture2D goldBadgeTex;
        private GUIStyle boxStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardValueCyanStyle;
        private GUIStyle cardValueGoldStyle;
        private GUIStyle cardValueGreenStyle;
        private GUIStyle cardSubtextStyle;
        private GUIStyle warnStyle;
        private bool isStylesInit = false;

        private void InitStyles()
        {
            if (isStylesInit) return;

            darkBoxTex = MakeBorderedTex(256, 64, new Color(0.04f, 0.06f, 0.10f, 0.94f), new Color(0.20f, 0.65f, 1.0f, 0.85f), 2);
            cardHeaderTex = MakePlainTex(2, 2, new Color(0.08f, 0.12f, 0.18f, 0.80f));
            dividerTex = MakePlainTex(1, 32, new Color(0.25f, 0.35f, 0.50f, 0.45f));
            goldBadgeTex = MakePlainTex(2, 2, new Color(0.85f, 0.65f, 0.10f, 0.95f));

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = darkBoxTex, textColor = Color.white },
                padding = new RectOffset(10, 10, 8, 8)
            };

            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.60f, 0.72f, 0.88f) },
                alignment = TextAnchor.MiddleCenter
            };

            cardValueCyanStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.88f, 1.0f) },
                alignment = TextAnchor.MiddleCenter
            };

            cardValueGoldStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.86f, 0.25f) },
                alignment = TextAnchor.MiddleCenter
            };

            cardValueGreenStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.95f, 0.55f) },
                alignment = TextAnchor.MiddleCenter
            };

            cardSubtextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.80f, 0.85f, 0.92f) },
                alignment = TextAnchor.MiddleCenter
            };

            warnStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = darkBoxTex, textColor = new Color(1f, 0.8f, 0.2f) },
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            isStylesInit = true;
        }

        private Texture2D MakePlainTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeBorderedTex(int w, int h, Color bg, Color border, int borderWidth)
        {
            Color[] pix = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x < borderWidth || x >= w - borderWidth || y < borderWidth || y >= h - borderWidth);
                    // Línea superior con mayor brillo neón de competición
                    bool isTopAccent = (y >= h - borderWidth * 2);
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
            InitStyles();

            // Marcador flotante panorámico de telemetría de alta velocidad
            float totalW = Mathf.Clamp(Screen.width * 0.62f, 680f, 840f);
            float totalH = 88f;
            float startX = (Screen.width - totalW) * 0.5f;
            float startY = 14f;

            // Marco principal estilo telemetría deportiva
            GUI.Box(new Rect(startX, startY, totalW, totalH), "", boxStyle);

            float colW = totalW / 3f;
            float km = currentDistanceMeters / 1000f;

            // --- COLUMNA 1: DISTANCIA RECORRIDA ---
            float col1X = startX;
            GUI.Label(new Rect(col1X, startY + 8f, colW, 18f), "📍 DISTANCIA EN PISTA", cardTitleStyle);
            GUI.Label(new Rect(col1X, startY + 24f, colW, 34f), $"{km:F2} <size=15>KM</size>", cardValueCyanStyle);
            GUI.Label(new Rect(col1X, startY + 58f, colW, 20f), $"{currentDistanceMeters:F0} m recorridos", cardSubtextStyle);

            // Divisor 1
            if (dividerTex != null)
            {
                GUI.DrawTexture(new Rect(startX + colW, startY + 16f, 1f, totalH - 32f), dividerTex);
            }

            // --- COLUMNA 2: VELOCIDAD PUNTA Y RÉCORD ---
            float col2X = startX + colW;
            GUI.Label(new Rect(col2X, startY + 8f, colW, 18f), "⚡ VELOCIDAD MÁXIMA SESIÓN", cardTitleStyle);
            GUI.Label(new Rect(col2X, startY + 24f, colW, 34f), $"{Mathf.RoundToInt(sessionTopSpeedKmh)} <size=15>KM/H</size>", cardValueGoldStyle);
            GUI.Label(new Rect(col2X, startY + 58f, colW, 20f), $"★ RÉCORD: {Mathf.RoundToInt(allTimeRecordKmh)} KM/H", cardSubtextStyle);

            // Divisor 2
            if (dividerTex != null)
            {
                GUI.DrawTexture(new Rect(startX + colW * 2f, startY + 16f, 1f, totalH - 32f), dividerTex);
            }

            // --- COLUMNA 3: OBSTÁCULOS ESQUIVADOS ---
            float col3X = startX + colW * 2f;
            GUI.Label(new Rect(col3X, startY + 8f, colW, 18f), "🛡️ OBSTÁCULOS ESQUIVADOS", cardTitleStyle);
            GUI.Label(new Rect(col3X, startY + 24f, colW, 34f), $"{obstaclesDodged} <size=15>EVADIDOS</size>", cardValueGreenStyle);
            string grade = (obstaclesDodged >= 20) ? "NIVEL: EXPERTO 🏆" : (obstaclesDodged >= 5 ? "NIVEL: AVANZADO 🔥" : "SIN IMPACTOS ✅");
            GUI.Label(new Rect(col3X, startY + 58f, colW, 20f), grade, cardSubtextStyle);

            // Si aún no hay auto asignado en la escena
            if (targetCar == null)
            {
                float warnW = 520f;
                float warnH = 65f;
                Rect warnRect = new Rect((Screen.width - warnW) * 0.5f, Screen.height * 0.35f, warnW, warnH);
                GUI.Box(warnRect, "⚠️ ¡Arrastra tu modelo/prefab del vehículo a la escena!\n(Asegúrate de que tenga el componente CarPhysicsController)", warnStyle);
            }
        }
    }
}
