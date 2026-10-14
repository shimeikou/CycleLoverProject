using CyclingLove.Content;
using UnityEngine;

namespace System.Service
{
    public static class CampaignContentLoader
    {
        public static CampaignDefinition LoadPrototype()
        {
            var source = UnityEngine.Resources.Load<TextAsset>("Content/PrototypeCampaign");
            if (source == null) throw new InvalidOperationException("Campaign content is missing");
            var definition = JsonUtility.FromJson<CampaignDefinition>(source.text);
            definition.Validate();
            return definition;
        }
    }
}
