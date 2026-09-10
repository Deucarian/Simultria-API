using System;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Deucarian.Simultria.UnityBuildRouting
{
    /// <summary>Recognizes only the documented explicit missing-record responses.</summary>
    internal static class SimultriaUnityBuildMissingRecordPolicy
    {
        internal static bool IsExplicitlyMissing(
            ApiResult<SimultriaResourceResponse<SimultriaUnityBuildVersionDto>> result,
            string buildVersion,
            string product)
        {
            if (result == null || result.IsSuccess || result.HttpStatusCode != 404 ||
                result.Error?.IsCancellation == true || result.Error?.IsTimeout == true ||
                result.Error?.Exception != null ||
                string.IsNullOrWhiteSpace(result.RawResponseBody))
            {
                return false;
            }

            try
            {
                var body = JToken.Parse(result.RawResponseBody) as JObject;
                if (body == null || !MatchesIfPresent(body, "version", buildVersion) ||
                    !MatchesIfPresent(body, "product", product) || body["data"] != null ||
                    body["error_code"] != null || body["errorCode"] != null || body["type"] != null)
                {
                    return false;
                }

                JToken code = body["code"];
                return code?.Type == JTokenType.String &&
                    (string.IsNullOrEmpty(result.Error?.BackendCode) ||
                        string.Equals(result.Error.BackendCode, "build_version_not_found",
                            StringComparison.Ordinal)) && string.Equals(
                        code.Value<string>(), "build_version_not_found",
                        StringComparison.Ordinal);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool MatchesIfPresent(JObject body, string key, string expected)
        {
            JToken value = body[key];
            return value == null || (value.Type == JTokenType.String &&
                string.Equals(value.Value<string>(), expected, StringComparison.Ordinal));
        }
    }
}
