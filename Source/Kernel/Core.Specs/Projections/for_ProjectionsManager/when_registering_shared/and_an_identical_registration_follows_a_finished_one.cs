// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_shared;

/// <summary>
/// Only registrations in flight at the same moment share their work. One that arrives after the first finished is a
/// new registration, so nothing a registration depends on is answered from a remembered result.
/// </summary>
public class and_an_identical_registration_follows_a_finished_one : given.a_projections_manager_grain
{
    IProjectionsManager _queuedRegistrations;

    void Establish()
    {
        _queuedRegistrations = Substitute.For<IProjectionsManager>();
        _silo.AddProbe(_ => _queuedRegistrations);
    }

    async Task Because()
    {
        static ProjectionDefinition[] Definitions() => [CreateDefinition("the-projection", "the-read-model")];

        await _grain.RegisterShared("the-signature", Definitions());
        await _grain.RegisterShared("the-signature", Definitions());
    }

    [Fact] void should_queue_each_as_a_registration_of_its_own() => _queuedRegistrations.ReceivedWithAnyArgs(2).Register(default!, default, default, default);
}
