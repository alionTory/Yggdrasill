using UnityEngine;

namespace Tests.E2eTests.ClickPointProvider
{
    public interface IClickPointProvider
    {
        Vector2 GetScreenPoint();
    }
}