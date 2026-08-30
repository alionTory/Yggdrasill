using UnityEngine;

namespace Yggdrasill.TestHooks.CalledByDebug
{
    public interface IClickPointProvider
    {
        Vector2 GetScreenPoint();
    }
}