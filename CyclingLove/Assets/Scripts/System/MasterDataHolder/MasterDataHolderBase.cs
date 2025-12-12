using Cysharp.Threading.Tasks;

namespace System.MasterDataHolder
{
    public abstract class MasterDataHolderBase
    {
        public abstract UniTask LoadSo();

        public abstract void Clear();
    }
}