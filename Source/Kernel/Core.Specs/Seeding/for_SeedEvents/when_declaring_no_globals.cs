// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_SeedEvents;

public class when_declaring_no_globals : given.a_seeding_grain
{
    Task Because() => new SeedEvents(TheEventStore, [], [], [], true, "program").Handle(_grainFactory);

    [Fact] void should_send_the_empty_authoritative_declaration() => _globalGrain.Received(1).SeedGlobalWithResult(Arg.Is<IEnumerable<SeedingEntry>>(_ => !_.Any()), true, "program");
    [Fact] void should_not_seed_a_namespace() => _namespaceGrain.DidNotReceive().SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>());
}
