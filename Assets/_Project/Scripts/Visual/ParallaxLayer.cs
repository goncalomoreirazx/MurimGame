using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Faz uma camada de fundo acompanhar a câmara mais devagar do que o resto do nível,
    /// para dar profundidade. factor = 1 fica colado à câmara (muito longe), 0 fica parado (junto à ação).
    /// </summary>
    [DefaultExecutionOrder(100)] // depois da câmara se mexer, para não tremer
    public class ParallaxLayer : MonoBehaviour
    {
        public Transform cameraTransform;
        public Vector2 factor = new Vector2(0.9f, 0.5f);

        Vector3 _startPosition;
        Vector3 _cameraStart;
        bool _initialized;

        /// <summary>Guarda a posição atual da camada e da câmara como referência.</summary>
        public void Init(Transform cam)
        {
            cameraTransform = cam;
            _startPosition = transform.position;
            _cameraStart = cam != null ? cam.position : Vector3.zero;
            _initialized = cam != null;
        }

        void Start()
        {
            if (!_initialized)
            {
                var cam = Camera.main;
                Init(cam != null ? cam.transform : null);
            }
        }

        void LateUpdate()
        {
            if (!_initialized || cameraTransform == null) return;
            Vector3 delta = cameraTransform.position - _cameraStart;
            transform.position = new Vector3(_startPosition.x + delta.x * factor.x,
                                             _startPosition.y + delta.y * factor.y,
                                             _startPosition.z);
        }
    }
}
