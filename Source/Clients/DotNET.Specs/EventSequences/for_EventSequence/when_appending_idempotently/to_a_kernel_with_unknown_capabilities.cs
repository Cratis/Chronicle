// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_idempotently;

public class to_a_kernel_with_unknown_capabilities : given.an_idempotent_append
{
    Exception _error;

    void Establish() => _capabilitiesKnown = false;

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.AppendOnce("source", "event", _key));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<IdempotentAppendsNotSupported>();
    [Fact] void should_not_send_anything() => _singleRequest.ShouldBeNull();
    [Fact] void should_not_advertise_support() => _eventSequence.SupportsIdempotentAppends.ShouldBeFalse();
}
