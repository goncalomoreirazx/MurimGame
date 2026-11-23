using UnityEngine;

namespace HeavenlyWarrior
{
    /// <summary>Troca os frames de um SpriteRenderer. Os scripts de cada personagem dizem que animação tocar.</summary>
    public class SpriteAnimator : MonoBehaviour
    {
        public SpriteRenderer target;
        public CharacterSprites sprites;

        string _current;
        float _time;
        float _fps;
        bool _loop;

        public string Current => _current;

        /// <summary>Toca uma animação em ciclo (ou parada no último frame se loop = false).</summary>
        public void Play(string anim, float fps = 10f, bool loop = true)
        {
            if (sprites == null) return;
            if (_current == anim && Mathf.Approximately(_fps, fps) && _loop == loop) return;
            if (!sprites.Has(anim)) return;
            _current = anim;
            _fps = fps;
            _loop = loop;
            _time = 0f;
            Apply();
        }

        /// <summary>Mostra um frame fixo (usado pelos golpes, que mudam de frame conforme a fase).</summary>
        public void ShowFrame(string anim, int index)
        {
            if (sprites == null) return;
            var frames = sprites.Get(anim);
            if (frames == null || frames.Length == 0) return;
            _current = anim;
            _fps = 0f;
            target.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        void Update()
        {
            if (sprites == null || _current == null || _fps <= 0f) return;
            _time += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            var frames = sprites.Get(_current);
            if (frames == null || frames.Length == 0 || target == null) return;
            int i = Mathf.FloorToInt(_time * _fps);
            i = _loop ? i % frames.Length : Mathf.Min(i, frames.Length - 1);
            target.sprite = frames[i];
        }
    }
}
