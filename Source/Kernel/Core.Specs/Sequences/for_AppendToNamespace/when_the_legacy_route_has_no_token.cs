// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendToNamespace;

public class when_the_legacy_route_has_no_token : given.a_protected_append_endpoint
{
    async Task Because() => await (_command.ToAppend("tenant") with { ExpectedIncarnation = null }).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_use_the_existing_append_method() => _eventSequence.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("Append");
    [Fact] void should_not_resolve_an_incarnation() => _incarnations.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_use_a_protected_method() => _forwarded.ShouldBeNull();
}
