using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>
    /// Congela o jogo durante uns milissegundos quando um golpe acerta, para dar peso ao impacto.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        static HitStop _instance;
        float _resumeAt;
        bool _active;

        public static void Do(float duration)
        {
            if (duration <= 0f) return;
            if (_instance == null)
            {
                var go = new GameObject("HitStop");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<HitStop>();
            }
            _instance._resumeAt = Mathf.Max(_instance._resumeAt, Time.unscaledTime + duration);
            _instance._active = true;
            Time.timeScale = 0f;
        }

        void Update()
        {
            if (_active && Time.unscaledTime >= _resumeAt)
            {
                _active = false;
                Time.timeScale = 1f;
            }
        }

        void OnDisable()
        {
            if (_active) Time.timeScale = 1f;
            _active = false;
        }
    }
}
