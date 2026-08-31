using System.Linq;
using Deucarian.Editor;
using Deucarian.Simultria.API.Editor;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class ControlCenterRegistrationTests
    {
        private const string PackageId =
            "com.deucarian.simultria-api";

        [Test]
        public void PackageRegistersStableToolAndCard()
        {
            Assert.That(
                DeucarianToolRegistry.TryGet(
                    DeucarianToolIds.SimultriaContractUpdater,
                    out DeucarianToolDescriptor tool),
                Is.True);
            Assert.That(tool.OwningPackage, Is.EqualTo(PackageId));

            DeucarianControlCenterSnapshot snapshot =
                DeucarianControlCenterSnapshotBuilder.Capture(true);
            Assert.That(
                snapshot.Cards.Any(
                    card => card.OwningPackage == PackageId),
                Is.True);
        }
        [Test]
        public void ContractCardReportsCoverageAndIncomingDrift()
        {
            var state = new SimultriaApiContractSnapshot(
                true,
                true,
                true,
                42,
                "1234567890abcdef1234");

            DeucarianControlCenterCard card =
                SimultriaApiDeveloperCardProvider.CreateCard(state);

            Assert.That(card.Status, Is.EqualTo(DeucarianControlCenterStatus.Warning));
            Assert.That(card.StatusText, Does.Contain("review required"));
            Assert.That(string.Join(" ", card.Details), Does.Contain("42"));
            Assert.That(string.Join(" ", card.Details), Does.Contain("coverage: complete"));
        }
    }
}
