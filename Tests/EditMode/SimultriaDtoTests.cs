using Deucarian.Simultria.API.Models;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaDtoTests
    {
        [Test]
        public void ProjectResponseMapsNestedModelAndVersionFields()
        {
            const string json =
                "{\"data\":{\"id\":1,\"name\":\"Project\",\"models\":[{" +
                "\"id\":11,\"name\":\"Model\",\"active_version\":{" +
                "\"id\":42,\"download_link\":\"https://example.invalid/model\"," +
                "\"version\":1,\"version_number\":2,\"order\":3}}]}}";

            SimultriaResourceResponse<SimultriaProjectDto> response =
                JsonConvert.DeserializeObject<
                    SimultriaResourceResponse<SimultriaProjectDto>>(json);

            Assert.That(response.Data.Id, Is.EqualTo(1));
            Assert.That(response.Data.Models, Has.Count.EqualTo(1));
            SimultriaModelVersionDto version =
                response.Data.Models[0].ActiveVersion;
            Assert.That(version.Id, Is.EqualTo(42));
            Assert.That(version.Version, Is.EqualTo("1"));
            Assert.That(version.VersionNumber, Is.EqualTo("2"));
            Assert.That(version.Order, Is.EqualTo("3"));
        }

        [Test]
        public void ScalarActiveVersionIsAcceptedAsReferenceId()
        {
            SimultriaModelDto model =
                JsonConvert.DeserializeObject<SimultriaModelDto>(
                    "{\"id\":11,\"active_version\":42}");

            Assert.That(model.ActiveVersion, Is.Not.Null);
            Assert.That(model.ActiveVersion.Id, Is.EqualTo(42));
        }

        [Test]
        public void ActivityMetadataDoesNotRequireIssuePayloadContract()
        {
            const string json =
                "{\"id\":3,\"external_id\":\"activity-3\"," +
                "\"title\":\"Review\",\"status\":\"to_do\"," +
                "\"issues\":[{\"id\":99,\"media\":[{\"id\":1}]}]}";

            SimultriaActivityDto activity =
                JsonConvert.DeserializeObject<SimultriaActivityDto>(json);

            Assert.That(activity.Id, Is.EqualTo(3));
            Assert.That(activity.Title, Is.EqualTo("Review"));
            Assert.That(activity.Status, Is.EqualTo("to_do"));
        }
    }
}
