using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Deucarian.Simultria.API.Models
{
    /// <summary>Project-model-version fields needed to resolve viewer content.</summary>
    [Serializable]
    public sealed class SimultriaModelVersionDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("project_model_id")]
        public int? ProjectModelId { get; set; }

        [JsonProperty("project_id")]
        public int? ProjectId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("frozen")]
        public bool? IsFrozen { get; set; }

        [JsonProperty("download_link")]
        public string DownloadUrl { get; set; }

        [JsonProperty("preview_image_url")]
        public string PreviewImageUrl { get; set; }

        [JsonConverter(typeof(SimultriaFlexibleStringConverter))]
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonConverter(typeof(SimultriaFlexibleStringConverter))]
        [JsonProperty("version_number")]
        public string VersionNumber { get; set; }

        [JsonConverter(typeof(SimultriaFlexibleStringConverter))]
        [JsonProperty("order")]
        public string Order { get; set; }

        [JsonProperty("latest_version")]
        public string LatestVersion { get; set; }

        [JsonProperty("latest_version_id")]
        public int? LatestVersionId { get; set; }

        [JsonProperty("active_version_id")]
        public int? ActiveVersionId { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset? CreatedAtUtc { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset? UpdatedAtUtc { get; set; }

        [JsonIgnore]
        public int? VersionNumberValue => ParseInteger(VersionNumber);

        [JsonIgnore]
        public int? OrderValue => ParseInteger(Order);

        private static int? ParseInteger(string value)
        {
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsed)
                ? parsed
                : (int?)null;
        }
    }
}
