using System.Collections.Generic;
using CyclingLove.Equipment;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scene.MainGrand
{
    public sealed class BikePreviewController : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler, IPointerClickHandler
    {
        private const int PreviewLayer = 31;
        private readonly List<Mesh> _runtimeMeshes = new();
        private UnityEngine.UI.RawImage _target;
        private RenderTexture _renderTexture;
        private GameObject _worldRoot;
        private GameObject _modelRoot;
        private Camera _camera;
        private Material _frameMaterial;
        private Material _componentMaterial;
        private Material _wheelMaterial;
        private Material _floorMaterial;
        private float _yaw;
        private float _pitch;
        private bool _initialized;

        public void Initialize(UnityEngine.UI.RawImage target)
        {
            if (_initialized) return;
            _initialized = true;
            _target = target;
            _target.raycastTarget = true;

            _renderTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32)
            {
                name = "BikePreviewRenderTexture",
                antiAliasing = 4,
                useMipMap = false,
                autoGenerateMips = false,
                hideFlags = HideFlags.DontSave
            };
            _renderTexture.Create();
            _target.texture = _renderTexture;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            _frameMaterial = CreateMaterial(shader, "BikePreviewFrame", new Color(0.12f, 0.48f, 0.78f));
            _componentMaterial = CreateMaterial(shader, "BikePreviewComponents", new Color(0.72f, 0.76f, 0.8f));
            _wheelMaterial = CreateMaterial(shader, "BikePreviewWheels", new Color(0.08f, 0.09f, 0.11f));
            _floorMaterial = CreateMaterial(shader, "BikePreviewFloor", new Color(0.12f, 0.15f, 0.17f));

            BuildWorld();
            ResetView();
        }

        public void SetLoadout(PlayerBikeEquipment loadout)
        {
            if (!_initialized || loadout == null) return;
            ApplyPalette(loadout);
            RebuildBike(loadout);
        }

        public void ResetView()
        {
            _yaw = -18f;
            _pitch = 4f;
            if (_camera != null) _camera.orthographicSize = 0.92f;
            ApplyRotation();
        }

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            _yaw -= eventData.delta.x * 0.35f;
            _pitch = Mathf.Clamp(_pitch + eventData.delta.y * 0.2f, -12f, 18f);
            ApplyRotation();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (_camera == null) return;
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize - eventData.scrollDelta.y * 0.055f, 0.68f, 1.35f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount >= 2) ResetView();
        }

        private void OnEnable()
        {
            if (_worldRoot != null) _worldRoot.SetActive(true);
            if (_camera != null) _camera.enabled = true;
        }

        private void OnDisable()
        {
            if (_camera != null) _camera.enabled = false;
            if (_worldRoot != null) _worldRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
            foreach (var mesh in _runtimeMeshes)
                if (mesh != null) Destroy(mesh);
            Destroy(_frameMaterial);
            Destroy(_componentMaterial);
            Destroy(_wheelMaterial);
            Destroy(_floorMaterial);
            if (_worldRoot != null) Destroy(_worldRoot);
        }

        private void BuildWorld()
        {
            _worldRoot = new GameObject("BikePreviewWorld") { hideFlags = HideFlags.DontSave };
            _worldRoot.transform.position = new Vector3(10000f, 10000f, 10000f);
            _worldRoot.layer = PreviewLayer;

            var cameraObject = new GameObject("BikePreviewCamera") { hideFlags = HideFlags.DontSave, layer = PreviewLayer };
            cameraObject.transform.SetParent(_worldRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(2.15f, 1.28f, -2.05f);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 0.92f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.035f, 0.055f, 0.07f, 1f);
            _camera.cullingMask = 1 << PreviewLayer;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 20f;
            _camera.targetTexture = _renderTexture;
            _camera.transform.LookAt(_worldRoot.transform.TransformPoint(new Vector3(0f, 0.53f, 0f)));

            var keyLightObject = new GameObject("KeyLight") { hideFlags = HideFlags.DontSave, layer = PreviewLayer };
            keyLightObject.transform.SetParent(_worldRoot.transform, false);
            keyLightObject.transform.localRotation = Quaternion.Euler(38f, -35f, 0f);
            var keyLight = keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.15f;
            keyLight.color = new Color(1f, 0.94f, 0.84f);
            keyLight.cullingMask = 1 << PreviewLayer;

            var fillLightObject = new GameObject("FillLight") { hideFlags = HideFlags.DontSave, layer = PreviewLayer };
            fillLightObject.transform.SetParent(_worldRoot.transform, false);
            fillLightObject.transform.localPosition = new Vector3(-1.4f, 1.2f, -0.5f);
            var fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 5f;
            fillLight.intensity = 2.4f;
            fillLight.color = new Color(0.52f, 0.72f, 1f);
            fillLight.cullingMask = 1 << PreviewLayer;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "PreviewFloor";
            floor.hideFlags = HideFlags.DontSave;
            floor.layer = PreviewLayer;
            floor.transform.SetParent(_worldRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.015f, 0f);
            floor.transform.localScale = new Vector3(0.22f, 1f, 0.22f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = _floorMaterial;
            DisableCollider(floor);
        }

        private void RebuildBike(PlayerBikeEquipment loadout)
        {
            if (_modelRoot != null)
            {
                _modelRoot.SetActive(false);
                Destroy(_modelRoot);
            }
            foreach (var mesh in _runtimeMeshes)
                if (mesh != null) Destroy(mesh);
            _runtimeMeshes.Clear();

            _modelRoot = new GameObject("BikeModel") { hideFlags = HideFlags.DontSave, layer = PreviewLayer };
            _modelRoot.transform.SetParent(_worldRoot.transform, false);

            var wheelbase = loadout.FramePartId == "frame_stag_classic_01" ? 1.08f :
                loadout.FramePartId == "frame_zenith_climber_01" ? 1.18f : 1.13f;
            var tubeRadius = loadout.FramePartId == "frame_zenith_climber_01" ? 0.022f :
                loadout.FramePartId == "frame_stag_classic_01" ? 0.032f : 0.027f;
            var rimRadius = loadout.WheelPartId == "wheels_heliocentric_aero_01" ? 0.052f :
                loadout.WheelPartId == "wheels_ryusho_value_01" ? 0.035f : 0.024f;

            var rearAxle = new Vector3(0f, 0.34f, -wheelbase * 0.5f);
            var frontAxle = new Vector3(0f, 0.34f, wheelbase * 0.5f);
            var bottomBracket = new Vector3(0f, 0.36f, -0.08f);
            var seatTop = new Vector3(0f, loadout.FramePartId == "frame_stag_classic_01" ? 0.79f : 0.86f, -0.2f);
            var headTop = new Vector3(0f, 0.82f, 0.34f);
            var headBottom = new Vector3(0f, 0.57f, 0.4f);

            var wheels = NewGroup("Wheels");
            BuildWheel(wheels.transform, rearAxle, rimRadius);
            BuildWheel(wheels.transform, frontAxle, rimRadius);

            var frame = NewGroup("Frame");
            CreateTube(frame.transform, rearAxle, seatTop, tubeRadius, _frameMaterial, "SeatStay");
            CreateTube(frame.transform, rearAxle, bottomBracket, tubeRadius, _frameMaterial, "ChainStay");
            CreateTube(frame.transform, bottomBracket, seatTop, tubeRadius * 1.12f, _frameMaterial, "SeatTube");
            CreateTube(frame.transform, seatTop, headTop, tubeRadius, _frameMaterial, "TopTube");
            CreateTube(frame.transform, bottomBracket, headBottom, tubeRadius * 1.18f, _frameMaterial, "DownTube");
            CreateTube(frame.transform, headBottom, headTop, tubeRadius * 1.05f, _frameMaterial, "HeadTube");
            CreateTube(frame.transform, headBottom, frontAxle, tubeRadius * 0.72f, _frameMaterial, "ForkLeft");
            CreateTube(frame.transform, headBottom + new Vector3(0.035f, 0f, 0f), frontAxle + new Vector3(0.035f, 0f, 0f), tubeRadius * 0.72f, _frameMaterial, "ForkRight");
            CreateTube(frame.transform, seatTop, seatTop + new Vector3(0f, 0.16f, -0.015f), tubeRadius * 0.65f, _componentMaterial, "SeatPost");
            CreateTube(frame.transform, seatTop + new Vector3(-0.15f, 0.16f, -0.015f), seatTop + new Vector3(0.15f, 0.16f, -0.015f), 0.026f, _componentMaterial, "Saddle");
            CreateTube(frame.transform, headTop, headTop + new Vector3(0f, 0.12f, 0.02f), 0.018f, _componentMaterial, "Stem");
            CreateTube(frame.transform, headTop + new Vector3(-0.2f, 0.12f, 0.02f), headTop + new Vector3(0.2f, 0.12f, 0.02f), 0.016f, _componentMaterial, "Handlebar");

            var components = NewGroup("Components");
            CreateTube(components.transform, bottomBracket + new Vector3(-0.055f, 0f, 0f), bottomBracket + new Vector3(0.055f, 0f, 0f), 0.105f, _componentMaterial, "Crankset");
            CreateTube(components.transform, bottomBracket + new Vector3(0.09f, 0f, 0f), bottomBracket + new Vector3(0.09f, -0.15f, 0.07f), 0.012f, _componentMaterial, "CrankArm");
            CreateTube(components.transform, bottomBracket + new Vector3(0.1f, 0.015f, -0.06f), rearAxle + new Vector3(0.1f, 0.015f, 0.04f), 0.008f, _componentMaterial, "Chain");
            if (loadout.ComponentsPartId == "components_quantum_electronic_01")
            {
                var control = GameObject.CreatePrimitive(PrimitiveType.Cube);
                control.name = "ElectronicControlUnit";
                control.layer = PreviewLayer;
                control.hideFlags = HideFlags.DontSave;
                control.transform.SetParent(components.transform, false);
                control.transform.localPosition = bottomBracket + new Vector3(0.08f, 0.1f, 0.02f);
                control.transform.localScale = new Vector3(0.07f, 0.09f, 0.05f);
                control.GetComponent<MeshRenderer>().sharedMaterial = _componentMaterial;
                DisableCollider(control);
            }

            ApplyRotation();
        }

        private GameObject NewGroup(string name)
        {
            var group = new GameObject(name) { hideFlags = HideFlags.DontSave, layer = PreviewLayer };
            group.transform.SetParent(_modelRoot.transform, false);
            return group;
        }

        private void BuildWheel(Transform parent, Vector3 center, float rimRadius)
        {
            var wheel = new GameObject(center.z < 0f ? "RearWheel" : "FrontWheel") { hideFlags = HideFlags.DontSave, layer = PreviewLayer };
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = center;

            var rim = new GameObject("Rim", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave, layer = PreviewLayer };
            rim.transform.SetParent(wheel.transform, false);
            var torus = CreateTorusMesh(0.34f, rimRadius, 32, 8);
            rim.GetComponent<MeshFilter>().sharedMesh = torus;
            rim.GetComponent<MeshRenderer>().sharedMaterial = _wheelMaterial;

            CreateTube(wheel.transform, new Vector3(-0.055f, 0f, 0f), new Vector3(0.055f, 0f, 0f), 0.035f, _componentMaterial, "Hub");
            for (var index = 0; index < 12; index++)
            {
                var angle = index * Mathf.PI * 2f / 12f;
                var end = new Vector3(0f, Mathf.Cos(angle) * 0.305f, Mathf.Sin(angle) * 0.305f);
                CreateTube(wheel.transform, Vector3.zero, end, 0.004f, _componentMaterial, "Spoke");
            }
        }

        private Mesh CreateTorusMesh(float majorRadius, float minorRadius, int majorSegments, int minorSegments)
        {
            var vertices = new Vector3[majorSegments * minorSegments];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[majorSegments * minorSegments * 6];
            for (var major = 0; major < majorSegments; major++)
            {
                var u = major * Mathf.PI * 2f / majorSegments;
                for (var minor = 0; minor < minorSegments; minor++)
                {
                    var v = minor * Mathf.PI * 2f / minorSegments;
                    var ring = majorRadius + minorRadius * Mathf.Cos(v);
                    var index = major * minorSegments + minor;
                    vertices[index] = new Vector3(minorRadius * Mathf.Sin(v), Mathf.Cos(u) * ring, Mathf.Sin(u) * ring);
                    normals[index] = new Vector3(Mathf.Sin(v), Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(u) * Mathf.Cos(v)).normalized;

                    var nextMajor = ((major + 1) % majorSegments) * minorSegments;
                    var nextMinor = (minor + 1) % minorSegments;
                    var triangle = index * 6;
                    triangles[triangle] = index;
                    triangles[triangle + 1] = nextMajor + minor;
                    triangles[triangle + 2] = nextMajor + nextMinor;
                    triangles[triangle + 3] = index;
                    triangles[triangle + 4] = nextMajor + nextMinor;
                    triangles[triangle + 5] = major * minorSegments + nextMinor;
                }
            }

            var mesh = new Mesh { name = "BikePreviewTorus", hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            _runtimeMeshes.Add(mesh);
            return mesh;
        }

        private static void CreateTube(Transform parent, Vector3 start, Vector3 end, float radius, Material material, string name)
        {
            var direction = end - start;
            var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tube.name = name;
            tube.layer = PreviewLayer;
            tube.hideFlags = HideFlags.DontSave;
            tube.transform.SetParent(parent, false);
            tube.transform.localPosition = (start + end) * 0.5f;
            tube.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            tube.transform.localScale = new Vector3(radius, direction.magnitude * 0.5f, radius);
            tube.GetComponent<MeshRenderer>().sharedMaterial = material;
            DisableCollider(tube);
        }

        private static void DisableCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        private static Material CreateMaterial(Shader shader, string name, Color color)
        {
            var material = new Material(shader) { name = name, color = color, hideFlags = HideFlags.DontSave };
            return material;
        }

        private void ApplyPalette(PlayerBikeEquipment loadout)
        {
            _frameMaterial.color = loadout.FramePartId switch
            {
                "frame_zenith_climber_01" => new Color(0.82f, 0.12f, 0.1f),
                "frame_stag_classic_01" => new Color(0.2f, 0.46f, 0.28f),
                _ => new Color(0.1f, 0.42f, 0.76f)
            };
            _componentMaterial.color = loadout.ComponentsPartId == "components_quantum_electronic_01"
                ? new Color(0.08f, 0.11f, 0.14f)
                : new Color(0.7f, 0.74f, 0.78f);
            _wheelMaterial.color = loadout.WheelPartId switch
            {
                "wheels_heliocentric_aero_01" => new Color(0.06f, 0.065f, 0.075f),
                "wheels_ryusho_value_01" => new Color(0.18f, 0.13f, 0.045f),
                _ => new Color(0.14f, 0.16f, 0.18f)
            };
        }

        private void ApplyRotation()
        {
            if (_modelRoot != null) _modelRoot.transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
