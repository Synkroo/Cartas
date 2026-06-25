using System;
using UnityEngine;

namespace JuegoDeCartas.Progression
{
    public interface IStableContentData
    {
        string ContentId { get; }
    }

    public abstract class StableContentData :
        ScriptableObject,
        IStableContentData
    {
        [SerializeField, HideInInspector]
        string contentId;

        public string ContentId =>
            string.IsNullOrWhiteSpace(contentId) ? name : contentId;

        protected void EnsureContentId()
        {
            if (string.IsNullOrWhiteSpace(contentId))
                contentId = Guid.NewGuid().ToString("N");
        }
    }

    public static class ContentIdUtility
    {
        public static string GetId(UnityEngine.Object asset)
        {
            if (asset is IStableContentData stable &&
                !string.IsNullOrWhiteSpace(stable.ContentId))
            {
                return stable.ContentId;
            }

            return asset != null ? asset.name : "";
        }
    }
}
