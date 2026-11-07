// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_shared;

/// <summary>
/// A client retrying a registration that just completed must be answered from the interleaved path. Queued behind the
/// subscription and recovery work of this non-reentrant grain it would time out against its own grain, and the retry
/// of that timeout is what kept a busy kernel from ever catching up (#5688).
/// </summary>
public class and_an_identical_registration_repeats_one_that_just_completed : given.a_projections_manager_grain
{
    IProjectionsManager _queuedRegistrations;
    string _signature;

    void Establish()
    {
        var definition = CreateDefinition("the-projection", "the-read-model");
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
        _signature = ProjectionRegistrationSignature.For([definition], null, null, null);
    }

    async Task Because()
    {
        await _grain.Register([CreateDefinition("the-projection", "the-read-model")]);

        _queuedRegistrations = Substitute.For<IProjectionsManager>();
        _silo.AddProbe(_ => _queuedRegistrations);

        await _grain.RegisterShared(_signature, [CreateDefinition("the-projection", "the-read-model")]);
    }

    [Fact] void should_not_queue_the_repeat_on_the_grain() => _queuedRegistrations.DidNotReceiveWithAnyArgs().Register(default!, default, default, default);
}
