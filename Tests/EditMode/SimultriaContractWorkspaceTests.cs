using Deucarian.Editor;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaContractWorkspaceTests
    {
        [Test]
        public void OpeningTheContractPageUsesNativeFieldsAndCannotApplyWithoutAPreview()
        {
            Assert.That(DeucarianToolRegistry.TryGet(DeucarianToolIds.SimultriaContractUpdater, out var tool), Is.True);
            using (var page = tool.CreatePage())
            {
                Assert.That(page.Root.Query<IMGUIContainer>().ToList(), Is.Empty);
                Assert.That(page.Root.Q<TextField>("contract-source"), Is.Not.Null);
                Assert.That(page.Root.Q<TextField>("contract-revision"), Is.Not.Null);
                Assert.That(page.Root.Q<Button>("contract-apply").enabledSelf, Is.False);
                Assert.That(page.Root.Q<Label>("contract-available").text, Is.EqualTo("Not checked"));
            }
        }
    }
}
