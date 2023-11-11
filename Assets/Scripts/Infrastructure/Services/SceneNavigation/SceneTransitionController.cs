using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.UI;

namespace MonsterFactory.Services.SceneManagement
{
    public interface ISceneTransitionController
    {
        public UniTask TransitionFadeInTask(SceneTransitionOptions options, bool keepFadeImageEnabled = true);
        public UniTask TransitionFadeOutTask(SceneTransitionOptions options, bool keepFadeImageEnabled = true);
    }

    public class SceneTransitionController : MonoBehaviour, ISceneTransitionController
    {
        [SerializeField] private Image fadeImage;

        private void Start()
        {
            EnableFadeImage(false);
        }

        #region API

        public async UniTask TransitionFadeInTask(SceneTransitionOptions options, bool keepFadeImageEnabled = true)
        {
            EnableFadeImage(true);
            UniTask fadeInImageTask = TransitionFadeTask(options.fadeColor, 0f, 1f, options.transitionDuration);
            await fadeInImageTask;
            EnableFadeImage(keepFadeImageEnabled);
        }

        public async UniTask TransitionFadeOutTask(SceneTransitionOptions options, bool keepFadeImageEnabled = true)
        {
            EnableFadeImage(true);
            UniTask fadeOutImageTask = TransitionFadeTask(options.fadeColor, 1f, 0f, options.transitionDuration);
            await fadeOutImageTask;
            EnableFadeImage(keepFadeImageEnabled);
        }

        #endregion


        #region Fade Image Accessors

        private void EnableFadeImage(bool isEnable)
        {
            if (fadeImage)
            {
                fadeImage.gameObject.SetActive(isEnable);
            }
        }

        private void SetAlphaFadeImage(float alpha)
        {
            if (!fadeImage) return;
            var fadeImageColor = fadeImage.color;
            fadeImageColor.a = alpha;
            SetColorFadeImage(fadeImageColor);
        }

        private void SetColorFadeImage(Color color)
        {
            if (fadeImage != null)
            {
                fadeImage.color = color;
            }
        }

        #endregion


        #region Implementation

        public UniTask TransitionFadeTask(Color fadeColor, float startValue, float endValue, float duration)
        {
            SetColorFadeImage(fadeColor);
            SetAlphaFadeImage(startValue);
            UniTask fadeImageTask = fadeImage?.DOFade(endValue, duration).Play().ToUniTask() ?? new UniTask();
            return fadeImageTask;
        }

        #endregion
    }
}