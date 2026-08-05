// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

/// <summary>
/// The read model index holds one handler per read model, so a read model claimed twice leaves the second projection
/// reachable only through a type of its own - and a model-bound projection has none.
/// </summary>
/// <remarks>
/// Both projections are still registered and both still write to the read model, which is the part worth knowing
/// about. It used to be resolved silently by declaration order: the model-bound projection simply vanished from the
/// index, so asking for it by its read model answered about the fluent one instead.
/// </remarks>
public class and_two_projections_maintain_the_same_read_model : given.all_dependencies
{
    ILogger<Projections> _logger;
    Projections _projections;

    async Task Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");

        _logger = Substitute.For<ILogger<Projections>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _clientArtifacts.Projections.Returns([typeof(TheFluentProjection)]);
        _clientArtifacts.ModelBoundProjections.Returns([typeof(TheContestedReadModel)]);
        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<TheContestedReadModel>>(typeof(TheFluentProjection))
            .Returns(new TheFluentProjection());

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            _logger);

        await _projections.Discover();
    }

    [Fact]
    void should_say_that_more_than_one_projection_claims_it() =>
        _logger.ReceivedWithAnyArgs().Log(LogLevel.Warning, default, default(object)!, null, default!);

    [Fact] void should_still_register_both() => _projections.Definitions.Count.ShouldEqual(2);

    public record TheContestedReadModel(string Id, string Name);

    public class TheFluentProjection : IProjectionFor<TheContestedReadModel>
    {
        public void Define(IProjectionBuilderFor<TheContestedReadModel> builder)
        {
        }
    }
}
