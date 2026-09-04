using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Yggdrasill.Utilities;

namespace Yggdrasill.TestHelper.CalledByDebug
{
    [RequireComponent(typeof(Collider2D))]
    public class Collider2DClickPointProvider : MonoBehaviour, IClickPointProvider, IValidatable
    {
        [SerializeField, HideInInspector] private Collider2D collider = null!;
        [SerializeField, HideInInspector] private Physics2DRaycaster raycaster = null!;
        [SerializeField, HideInInspector] private Camera worldCamera = null!;

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
            if (collider == null) TryGetComponent(out collider);
            if (raycaster == null)
            {
                raycaster = FindAnyObjectByType<Physics2DRaycaster>();
                worldCamera = raycaster.eventCamera;
            }

            this.LogError();
        }

        public Vector2 GetScreenPoint()
        {
            return worldCamera.WorldToScreenPoint(collider.bounds.center);
        }
    }
}