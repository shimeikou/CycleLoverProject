using System;
using System.GameSession;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene.Init
{
    public class InitSceneModel 
    {
        public async UniTask Init()
        {
            Application.targetFrameRate = 60;
            await GameSession.InitConfig();
        }
    }
}
