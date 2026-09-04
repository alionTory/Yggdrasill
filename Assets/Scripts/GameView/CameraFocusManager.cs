using System;
using System.Collections.Generic;
using UnityEngine;
using Yggdrasill.Utilities;

namespace Yggdrasill.GameView
{

	[RequireComponent(typeof(Camera))]
    public class CameraFocusManager : MonoBehaviour, IValidatable
    {
        [Tooltip("씬 시작 시 카메라에서 보이게 하고 싶은 월드 좌표")]
        public Vector2 targetPoint;

        [Tooltip("뷰포트 상에서 targetPoint가 위치할 지점, (0,0)=완전 좌하단. (1,1)=완전 최상단")]
        public Vector2 viewportPosition;
        
        [SerializeField, HideInInspector]
        private Camera camera = null!;
        
        public List<string> Validate()
        {
            var result = new List<string>();
            IValidatable.CheckNotNull(camera, result);
            if (!camera.orthographic)
                result.Add("카메라가 orthographic이여야 합니다.");
            
            return result;
        }

        private void OnValidate()
        {
            if (camera==null) TryGetComponent<Camera>(out camera);
            this.LogError();
        }

        private void Awake()
        {
            PlaceTargetAtViewportPosition();
        }

        private void PlaceTargetAtViewportPosition()
        {

            float viewHeight = 2f * camera.orthographicSize;
            float viewWidth = viewHeight * camera.aspect;

            Vector2 viewSize = new Vector2(viewWidth, viewHeight);

            // targetPoint가 viewportPosition 위치에 오도록 카메라 위치 역산
            Vector2 offset = (viewportPosition - new Vector2(0.5f, 0.5f)) * viewSize;
            Vector3 newCamPos = new Vector3(
                targetPoint.x - offset.x,
                targetPoint.y - offset.y,
                transform.position.z // z는 그대로 유지 (2D에서는 보통 -10)
            );

            transform.position = newCamPos;
        }
    }
}