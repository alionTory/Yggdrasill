using System.Threading;
using System.Threading.Tasks;
using QuantumUser.View;

namespace Tests.E2eTests
{
    public interface ITestHookApi
    {
        public Task ClickObject(GameObjectId gameObjectId);

        public Task WaitUntilSceneLoad(SceneInfo sceneInfo, CancellationToken cancellationToken);
    }
}