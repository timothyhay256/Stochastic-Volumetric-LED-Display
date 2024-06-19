using System;
using Newtonsoft.Json;
using Unity.Services.Core.Environments.Client.Models;

namespace Unity.Services.Core.Editor.Environments
{
    /// <summary>
    /// The struct defining the EnvironmentInfo.
    /// </summary>
    public struct EnvironmentInfo
    {
        /// <summary>
        /// Name of the environment.
        /// </summary>
        [JsonProperty("name")]
        public readonly string Name;
        /// <summary>
        /// Guid of the environment.
        /// </summary>
        [JsonProperty("id")]
        public readonly Guid Id;
        /// <summary>
        /// If the environment is the default environment.
        /// </summary>
        [JsonProperty("isDefault")]
        public readonly bool IsDefault;

        /// <summary>
        ///
        /// </summary>
        /// <param name="name">The name of the environment.</param>
        /// <param name="id">The guid of the environment.</param>
        /// <param name="isDefault">If the environment is a default environment.</param>
        public EnvironmentInfo(string name, Guid id, bool isDefault)
        {
            Name = name;
            Id = id;
            IsDefault = isDefault;
        }

        internal EnvironmentInfo(UnityEnvironmentV1 unityEnvironment)
        {
            Name = unityEnvironment.Name;
            Id = unityEnvironment.Id;
            IsDefault = unityEnvironment.IsDefault;
        }
    }
}
