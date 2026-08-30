using System.Collections.Generic;
using UnityEngine;
using Yggdrasill.Utilities;

namespace Yggdrasill.TestHelper.CalledByDebug
{
    [RequireComponent(typeof(RectTransform))]
    public class UGuiClickPointProvider : MonoBehaviour, IClickPointProvider, IValidatable
    {
        [SerializeField, HideInInspector] private RectTransform rectTransform = null!;
        [SerializeField, HideInInspector] private Canvas canvas = null!;

        public List<string> Validate()
        {
            var result = new List<string>();
            IValidatable.CheckNotNull(rectTransform, result);
            IValidatable.CheckNotNull(canvas, result);
            return result;
        }

        private void OnValidate()
        {
            if (rectTransform == null) TryGetComponent(out rectTransform);
            if(canvas == null) canvas = GetComponentInParent<Canvas>();
            this.LogError();
        }

        public Vector2 GetScreenPoint()
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners); // 회전, 스케일이 반영된 실제 모서리
            Vector3 center = (corners[0] + corners[2]) * 0.5f;

            Vector2 result;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                result = new Vector2(center.x, center.y);
            else
                result = canvas.worldCamera.WorldToScreenPoint(center);

            return result;
        }
    }
}