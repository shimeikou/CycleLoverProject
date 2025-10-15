using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene.Init
{
    public class InitSceneModel 
    {

        public async UniTask Init()
        {
            //まああれよ。バージョン更新確認とかよ
            await UniTask.DelayFrame(1);
            return;
        }

        public void Update()
        {
            //データリフレッシュ
        }
        
    }
}
