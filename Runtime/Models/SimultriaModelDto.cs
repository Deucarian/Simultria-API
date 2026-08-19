using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Deucarian.Simultria.API.Models
{
    /// <summary>Project-model fields used for viewer model selection.</summary>
    [Serializable]
    public sealed class SimultriaModelDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("preview_image_url")]
        public string PreviewImageUrl { get; set; }

        [JsonConverter(typeof(SimultriaModelVersionReferenceConverter))]
        [JsonProperty("active_version")]
        public SimultriaModelVersionDto ActiveVersion { get; set; }

        [JsonConverter(typeof(SimultriaModelVersionReferenceConverter))]
        [JsonProperty("frozen_version")]
        public SimultriaModelVersionDto FrozenVersion { get; set; }

        [JsonProperty("model_versions")]
        public List<SimultriaModelVersionDto> ModelVersions { get; set; } =
            new List<SimultriaModelVersionDto>();

        [JsonProperty("versions")]
        public List<SimultriaModelVersionDto> Versions { get; set; } =
            new List<SimultriaModelVersionDto>();

        [JsonProperty("created_at")]
        public DateTimeOffset? CreatedAtUtc { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }
}
