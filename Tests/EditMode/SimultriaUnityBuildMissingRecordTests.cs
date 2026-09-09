using System;
using System.Threading.Tasks;
using Deucarian.API;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.UnityBuildRouting;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaUnityBuildMissingRecordTests
    {
        [TestCase("{\"code\":\"build_version_not_found\"}")]
        [TestCase("{\"code\":\"build_version_not_found\",\"version\":\"1.0\",\"product\":\"activity_viewer\"}")]
        public async Task OnlyExplicitMissingRecordExposesFallbackSignal(string body)
        {
            var result = await ResolveFailure(new ApiError { HttpStatusCode = 404, RawResponseBody = body });

            Assert.That(result.IsVersionMissing, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo("build_version_not_found"));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.EnvironmentId.IsEmpty, Is.True, "The viewer owns any fallback decision.");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Not found")]
        [TestCase("<html>Not found</html>")]
        [TestCase("[]")]
        [TestCase("{\"message\":\"Not Found\"}")]
        [TestCase("{\"message\":\"No active Unity Build version for activity_viewer found on this current environment.\"}")]
        [TestCase("{\"version\":\"1.0\",\"product\":\"activity_viewer\",\"message\":\"No active Unity Build version for activity_viewer found on this current environment.\"}")]
        [TestCase("{\"version\":\"1.0\",\"product\":\"activity_viewer\",\"message\":\"Not found\"}")]
        [TestCase("{\"code\":\"unsupported_product\"}")]
        [TestCase("{\"code\":\"build_version_deprecated\"}")]
        [TestCase("{\"code\":\"build_version_not_found\",\"data\":{\"environment\":\"deprecated\"}}")]
        [TestCase("{\"error_code\":\"unsupported_product\",\"message\":\"No active Unity Build version for activity_viewer found on this current environment.\"}")]
        [TestCase("{\"code\":\"build_version_not_found\",\"product\":\"report_viewer\"}")]
        [TestCase("{\"code\":\"build_version_not_found\",\"version\":\"2.0\"}")]
        [TestCase("{\"code\":\"build_version_not_found\",\"version\":null}")]
        [TestCase("{\"message\":\"No active Unity Build version for report_viewer found on this current environment.\"}")]
        [TestCase("No active Unity Build version for activity_viewer found on this current environment.")]
        [TestCase("{\"code\":\"unsupported_product\",\"message\":\"No active Unity Build version for activity_viewer found on this current environment.\"}")]
        public async Task Other404ResponsesCannotAuthorizeFallback(string body)
        {
            var result = await ResolveFailure(new ApiError { HttpStatusCode = 404, RawResponseBody = body });
            Assert.That(result.IsVersionMissing, Is.False);
            Assert.That(result.Succeeded, Is.False);
        }

        [TestCase(0)]
        [TestCase(401)]
        [TestCase(403)]
        [TestCase(410)]
        [TestCase(429)]
        [TestCase(500)]
        public async Task Non404StatusCannotAuthorizeFallback(int status)
        {
            var result = await ResolveFailure(new ApiError
            {
                HttpStatusCode = status, RawResponseBody = "{\"code\":\"build_version_not_found\"}"
            });
            Assert.That(result.IsVersionMissing, Is.False);
        }

        [Test]
        public async Task TimeoutCancellationAndExceptionCannotAuthorizeFallback()
        {
            foreach (var error in new[]
            {
                new ApiError { IsTimeout = true },
                new ApiError { IsCancellation = true },
                new ApiError { Exception = new InvalidOperationException("safe test error") }
            })
            {
                error.HttpStatusCode = 404;
                error.RawResponseBody = "{\"code\":\"build_version_not_found\"}";
                Assert.That((await ResolveFailure(error)).IsVersionMissing, Is.False);
            }
        }

        [Test]
        public async Task ThrownNetworkFailureIsSanitizedAndDoesNotAuthorizeFallback()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var client = new ApiClientSpy { ThrownException = new InvalidOperationException("private details") };
                var result = await new SimultriaUnityBuildRoutingService(client, fixture.Composition)
                    .ResolveAsync("1.0", "activity_viewer");
                Assert.That(result.IsVersionMissing, Is.False);
                Assert.That(result.Message, Does.Not.Contain("private details"));
            }
        }

        [TestCase("2.0", "activity_viewer", "development", "build_version_mismatch")]
        [TestCase("1.0", "report_viewer", "development", "build_product_mismatch")]
        [TestCase("1.0", "activity_viewer", "deprecated", "build_environment_unknown")]
        [TestCase("1.0", "activity_viewer", "unknown", "build_environment_unknown")]
        public void SuccessfulTransportDoesNotRelaxExactIdentityOrEnvironment(
            string version, string product, string environment, string expectedCode)
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var result = new SimultriaUnityBuildRoutingService(null, fixture.Composition)
                    .EvaluateLookupResult("1.0", "activity_viewer", ApiResult<SimultriaResourceResponse<
                        SimultriaUnityBuildVersionDto>>.Success(
                        new SimultriaResourceResponse<SimultriaUnityBuildVersionDto>
                        {
                            Data = new SimultriaUnityBuildVersionDto
                            {
                                Version = version, Product = product, Environment = environment
                            }
                        }, HttpMethod.GET, 200, null, null));
                Assert.That(result.ErrorCode, Is.EqualTo(expectedCode));
                Assert.That(result.IsVersionMissing, Is.False);
            }
        }

        private static async Task<SimultriaUnityBuildRoutingResult> ResolveFailure(ApiError error)
        {
            using (var fixture = new SimultriaTestComposition())
            {
                return await new SimultriaUnityBuildRoutingService(
                        new ApiClientSpy { ResponseError = error }, fixture.Composition)
                    .ResolveAsync("1.0", "activity_viewer");
            }
        }
    }
}
