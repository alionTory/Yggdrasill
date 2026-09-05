using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yggdrasill.Utilities
{
    /// <summary>
    /// 원본 이미지의 비율을 구해, AspectRatioFitter의 aspect ratio 값을 자동으로 설정해 준다.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.AspectRatioFitter))]
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class AspectAutoSetter : MonoBehaviour, IValidatable
    {
        [SerializeField, HideInInspector] private Image image = null!;
        [SerializeField, HideInInspector] private AspectRatioFitter aspectRatioFitter = null!;

        public List<string> Validate()
        {
            var result = new List<string>();
            this.CheckNotNullIfInScene(image, result);
            this.CheckNotNullIfInScene(aspectRatioFitter, result);
            
            if (result.Count == 0)
            {
                var imageAspectRatio = CalculateImageAspectRatio();
                if (MathF.Abs(aspectRatioFitter.aspectRatio - imageAspectRatio) > 1e-6f)
                {
                    result.Add(
                        $"AspectRatioFitter에 부여된 aspect ratio 값 {aspectRatioFitter.aspectRatio}가 원본 이미지의 비율 {imageAspectRatio}과 다릅니다.");
                }
            }

            return result;
        }

        private float CalculateImageAspectRatio()
        {
            var rect = image.sprite.rect;
            if (rect.height == 0f)
                return 1f;
            return rect.width / rect.height;
        }
        
        private void OnValidate()
        {
            if(image == null) TryGetComponent(out image);
            if(aspectRatioFitter == null) TryGetComponent(out aspectRatioFitter);
            aspectRatioFitter.aspectRatio = CalculateImageAspectRatio();
        }
    }
}