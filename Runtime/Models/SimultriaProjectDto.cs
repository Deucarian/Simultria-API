using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Deucarian.Simultria.API.Models
{
    /// <summary>Project fields used for viewer project/model selection.</summary>
    [Serializable]
    public sealed class SimultriaProjectDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("preview_image")]
        public string PreviewImageUrl { get; set; }

        [JsonProperty("model_count")]
        public int? ModelCount { get; set; }

        [JsonProperty("models")]
        public List<SimultriaModelDto> Models { get; set; } =
            new List<SimultriaModelDto>();

        [JsonProperty("project_count")]
        public int? SubProjectCount { get; set; }

        [JsonProperty("sub_projects")]
        public List<SimultriaProjectDto> SubProjects { get; set; } =
            new List<SimultriaProjectDto>();

        [JsonProperty("created_at")]
        public DateTimeOffset? CreatedAtUtc { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }
}
