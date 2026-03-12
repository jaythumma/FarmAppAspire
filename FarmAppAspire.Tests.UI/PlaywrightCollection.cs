using FarmAppAspire.Tests.UI.Fixtures;

[assembly: AssemblyFixture(typeof(AspirePlaywrightFixture))]

namespace FarmAppAspire.Tests.UI;

[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<AspirePlaywrightFixture> { }
