using System;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapCollider2D))]
public class TilemapView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Tilemap _tilemap;

    private void Reset()
    {
        _tilemap = GetComponent<Tilemap>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) // 좌클릭 시에만 실행
            return;
        
        Vector3 clickPos = eventData.pointerCurrentRaycast.worldPosition;
        Vector3Int cellPos = _tilemap.WorldToCell(clickPos);
        Vector3 cellCenterWorldPos =  _tilemap.GetCellCenterWorld(cellPos);
        
        TileClickedCommand command = new TileClickedCommand
        {
            worldPosition = new FPVector2(FP.FromRoundedFloat_UNSAFE(cellCenterWorldPos.x), FP.FromRoundedFloat_UNSAFE(cellCenterWorldPos.y))
        };
        QuantumRunner.Default.Game.SendCommand(command);
    }
}