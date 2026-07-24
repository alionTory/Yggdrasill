using System.Collections.Generic;
using Quantum;
using QuantumUser.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapCollider2D))]
[RequireComponent(typeof(Tilemap))]
public class TilemapView : MonoBehaviour, IPointerClickHandler, IValidatable
{
    [SerializeField] private Tilemap tilemap = null!;

    public List<string> Validate()
    {
        var result = new List<string>();
        IValidatable.CheckNotNull(tilemap, result);
        return result;
    }

    private void OnValidate()
    {
        tilemap = GetComponent<Tilemap>();
        foreach (var errorMessage in Validate())
        {
            Debug.LogError(errorMessage, this);
        }
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
        Vector3 cellCenterWorldPos = tilemap.GetCellCenterWorld(cellPos);

        var command = SpawnSeedlingCommand.CreateFromView(cellCenterWorldPos.x, cellCenterWorldPos.y);
        QuantumRunner.Default.Game.SendCommand(command);
    }
}