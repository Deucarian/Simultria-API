using System;
using Newtonsoft.Json;

namespace Deucarian.Simultria.API.Models
{
    /// <summary>
    /// Credential-free fields returned by the public Unity build directory.
    /// Additional backend configuration fields are intentionally ignored.
    /// </summary>
    [Serializable]
    public sealed class SimultriaUnityBuildVersionDto
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("product")]
        public string Product { get; set; }

        [JsonProperty("environment")]
        public string Environment { get; set; }
    }
}
