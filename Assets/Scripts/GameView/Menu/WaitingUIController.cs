using System;
using Quantum;
using UnityEngine;

namespace QuantumUser.View.Menu
{
    public class WaitingUIController :MonoBehaviour
    {
        private void Awake()
        {
            QuantumEvent.Subscribe<EventGameStateChanged>(this, Toggle);
        }
        
        private void Toggle(EventGameStateChanged eventGameStateChanged)
        {
            if (eventGameStateChanged.NewState == GameState.Pending)
                gameObject.SetActive(true);
            else if(eventGameStateChanged.NewState == GameState.Running)
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            QuantumEvent.UnsubscribeListener(this);
        }
    }
}