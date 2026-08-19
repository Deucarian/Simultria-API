using System;
using Newtonsoft.Json;

namespace Deucarian.Simultria.API.Models
{
    /// <summary>
    /// Activity metadata shared by viewers. Issue/media payloads are
    /// intentionally not part of this package contract.
    /// </summary>
    [Serializable]
    public sealed class SimultriaActivityDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("external_id")]
        public string ExternalId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("assigned_to")]
        public SimultriaUserSummaryDto AssignedTo { get; set; }

        [JsonProperty("planned_date")]
        public string PlannedDate { get; set; }

        [JsonProperty("completion_date")]
        public string CompletionDate { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset? CreatedAtUtc { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }

    /// <summary>Minimal user reference embedded by activity responses.</summary>
    [Serializable]
    public sealed class SimultriaUserSummaryDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
