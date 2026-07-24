using System;
using System.Collections.Generic;
using QuantumUser.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tests.E2eTests.ClickPointProvider
{
    [RequireComponent(typeof(Collider2D))]
    public class Collider2DClickPointProvider : MonoBehaviour, IClickPointProvider, IValidatable
    {
        [SerializeField] private Collider2D collider = null!;
        [SerializeField] private Physics2DRaycaster raycaster = null!;
        [SerializeField] private Camera worldCamera = null!;

        public List<string> Validate()
        {
            var result = new List<string>();
            IValidatable.CheckNotNull(collider, result);
            IValidatable.CheckNotNull(raycaster, result);
            IValidatable.CheckNotNull(worldCamera, result);
            return result;
        }

        private void OnValidate()
        {
            collider = GetComponent<Collider2D>();
            raycaster = FindAnyObjectByType<Physics2DRaycaster>();
            worldCamera = raycaster.eventCamera;
            this.LogError();
        }

        public Vector2 GetScreenPoint()
        {
            return worldCamera.WorldToScreenPoint(collider.bounds.center);
        }
    }
}