using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Deucarian.Simultria.API.Models
{
    /// <summary>Standard Simultria response containing one resource.</summary>
    [Serializable]
    public sealed class SimultriaResourceResponse<T>
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }

    /// <summary>Standard Simultria response containing a resource list.</summary>
    [Serializable]
    public sealed class SimultriaCollectionResponse<T>
    {
        [JsonProperty("data")]
        public List<T> Data { get; set; } = new List<T>();
    }
}
