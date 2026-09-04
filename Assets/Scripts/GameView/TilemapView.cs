using System;
using System.Collections.Generic;
using Quantum;
using Yggdrasill.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace Yggdrasill.GameView
{
    [RequireComponent(typeof(TilemapCollider2D))]
    [RequireComponent(typeof(Tilemap))]
    public class TilemapView : MonoBehaviour, IPointerClickHandler, IValidatable
    {
        [SerializeField, HideInInspector] private Tilemap tilemap = null!;

        public List<string> Validate()
        {
            var result = new List<string>();
            IValidatable.CheckNotNull(tilemap, result);
            return result;
        }

        private void OnValidate()
        {
            if (tilemap == null) TryGetComponent(out tilemap);
            this.LogError();
        }

        /// <summary>
        /// 플레이어가 타일맵의 타일을 클릭하면, 클릭한 타일의 중앙 월드 좌표에 대해 SpawnSeedlingCommand를 생성하고, QuantumRunner.Default.Game.SendCommand()을 통해 서버에 전송함.
        /// </summary>
        /// <remarks>
        /// (마우스 클릭일 경우) 왼쪽 클릭 이벤트 시에만 실행됨. 중앙, 오른쪽 클릭은 무시함.
        /// </remarks>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) // 좌클릭 시에만 실행
                return;

            Vector3 clickPos = eventData.pointerCurrentRaycast.worldPosition;
            Vector3Int cellPos = tilemap.WorldToCell(clickPos);
            Debug.Log($"셀 클릭됨. {cellPos}", this);
            Vector3 cellCenterWorldPos = tilemap.GetCellCenterWorld(cellPos);

            var command = SpawnSeedlingCommand.CreateFromView(cellCenterWorldPos.x, cellCenterWorldPos.y);
            QuantumRunner.Default.Game.SendCommand(command);
        }

#if DEBUG
        /// <summary>
        /// 격자(타일맵)의 <paramref name="cell.x"/>열 <paramref name="cell.y"/>행 칸의 중앙에 해당하는 화면 좌표를 반환한다.
        /// </summary>
        /// <exception cref="Exception">
        /// 해당 칸의 중앙이 화면 밖에 있어 클릭할 수 없으면 예외 발생.
        /// </exception>
        /// <param name="cell">
        /// 가장 왼쪽 열의 칸은 <paramref name="cell.x"/>==1, 가장 아래 행의 칸은 <paramref name="cell.y"/>==1.
        /// </param>
        public Vector2 GetTileClickPosition(Vector2Int cell)
        {
            var eventCamera = GetEventCamera();

            var cellCenterWorld = tilemap.GetCellCenterWorld(new Vector3Int(cell.x - 1, cell.y - 1, 0));
            Vector2 clickPoint = eventCamera.WorldToScreenPoint(cellCenterWorld);

            if (clickPoint.x < 0 || clickPoint.x > Screen.width || clickPoint.y < 0 || clickPoint.y > Screen.height)
                throw new Exception(
                    $"타일 ({cell.x}, {cell.y})의 중앙에 해당하는 화면 좌표 {clickPoint}가 " +
                    $"화면({Screen.width}x{Screen.height}) 밖이므로 클릭할 수 없음.");

            return clickPoint;
        }

        private Camera? _eventCamera;

        /// <summary>
        /// 월드 공간의 클릭 이벤트를 처리하는 카메라를 조회한다.
        /// </summary>
        /// <exception cref="Exception">
        /// 현재 씬에 <see cref="Physics2DRaycaster"/>가 없으면 예외 발생.
        /// </exception>
        private Camera GetEventCamera()
        {
            if (_eventCamera == null)
            {
                var raycaster = FindAnyObjectByType<Physics2DRaycaster>();
                if (raycaster == null)
                    throw new Exception($"현재 씬에 {nameof(Physics2DRaycaster)}가 없음.");
                _eventCamera = raycaster.eventCamera;
            }

            return _eventCamera;
        }

        /// <summary>
        /// 월드 좌표를 격자(타일맵)의 칸 좌표로 변환한다. <br/>
        /// </summary>
        public Vector2Int WorldToCellPosition(Vector2 worldPosition)
        {
            var worldToCellResult = tilemap.WorldToCell(worldPosition);
            return new Vector2Int(worldToCellResult.x + 1, worldToCellResult.y + 1);
        }

#endif
    }
}
