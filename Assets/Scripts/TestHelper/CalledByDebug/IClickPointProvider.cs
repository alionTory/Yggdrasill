using UnityEngine;

namespace Yggdrasill.TestHooks.ClickPoints
{
    public interface IClickPointProvider
    {
        Vector2 GetScreenPoint();
    }
}