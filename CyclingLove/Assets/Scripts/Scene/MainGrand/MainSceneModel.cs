using System.Event;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Scene.MainGrand
{
    public class MainSceneModel
    {
        public async UniTask Initialize()
        {
            await LoadSchedule();
        }

        private async UniTask LoadSchedule()
        {
           
        }

        public GameEvent GetCurrentEvent()
        {

            return new GameEvent();
        }
    }
}