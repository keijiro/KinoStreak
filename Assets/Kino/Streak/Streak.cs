using UnityEngine;

namespace Kino
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Kino Image Effects/Streak")]
    public class Streak : MonoBehaviour
    {
        #region Editable variables and public properties

        [SerializeField, Range(0, 1)] float _intensity = 0.3f;

        public float intensity {
            get { return _intensity; }
            set { _intensity = value; }
        }

        [SerializeField, Range(0, 5)] float _threshold = 1;

        public float threshold {
            get { return _threshold; }
            set { _threshold = value; }
        }

        [SerializeField, ColorUsage(false)]
        Color _color = new Color(0.55f, 0.55f, 1);

        public Color color {
            get { return _color; }
            set { _color = value; }
        }

        #endregion

        #region Private variables

        [SerializeField, HideInInspector] Shader _shader;
        Material _material;

        #endregion

        #region MonoBehaviour functions

        void OnDestroy()
        {
            if (_material != null)
            {
                if (Application.isPlaying)
                    Destroy(_material);
                else
                    DestroyImmediate(_material);
            }
        }

        RenderTexture GetTempRT(int width, int height)
        {
            var format = RenderTextureFormat.ARGBHalf;
            var rt = RenderTexture.GetTemporary(width, height, 0, format);
            return rt;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material == null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.DontSave;
            }

            _material.SetFloat("_Intensity", _intensity);
            _material.SetFloat("_Threshold", _threshold);
            _material.SetColor("_Color", _color);

            var width = source.width;
            var height = source.height / 2;

            var filtered = GetTempRT(width, height);
            source.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, filtered, _material, 0);

            var downBuffers = new RenderTexture[32];
            var mipCount = 0;
            var prev = filtered;

            while (width > 16)
            {
                width /= 2;
                var rt = GetTempRT(width, height);
                Graphics.Blit(prev, rt, _material, 1);
                downBuffers[mipCount] = rt;
                prev = rt;
                mipCount++;
            }

            for (var i = mipCount - 1; i > 0; i--)
            {
                var hi = downBuffers[i - 1];
                var rt = GetTempRT(hi.width, hi.height);
                rt.filterMode = FilterMode.Bilinear;
                _material.SetTexture("_HighTex", hi);
                Graphics.Blit(prev, rt, _material, 2);
                RenderTexture.ReleaseTemporary(prev);
                prev = rt;
            }

            _material.SetTexture("_HighTex", source);
            Graphics.Blit(prev, destination, _material, 3);

            for (var i = 0; i < mipCount - 1; i++)
                RenderTexture.ReleaseTemporary(downBuffers[i]);
            RenderTexture.ReleaseTemporary(prev);
            RenderTexture.ReleaseTemporary(filtered);
        }

        #endregion
    }
}
