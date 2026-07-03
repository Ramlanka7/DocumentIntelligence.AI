namespace AI.DocumentIntelligence.Tests.Integration;

/// <summary>
/// xUnit collection marker that prevents parallel execution among integration tests that
/// share an <see cref="ApiWebApplicationFactory"/> instance. Because the factory holds
/// in-memory repositories that tests mutate (seed users, documents), tests must run
/// sequentially to avoid data cross-contamination.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<ApiWebApplicationFactory>;
