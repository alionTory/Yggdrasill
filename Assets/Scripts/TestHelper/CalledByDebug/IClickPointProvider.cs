using UnityEngine;

namespace Yggdrasill.TestHelper.CalledByDebug
{
    public interface IClickPointProvider
    {
        Vector2 GetScreenPoint();
    }
}