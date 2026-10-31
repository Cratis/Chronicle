// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventMetadata.when_reading_metadata_at;

public class and_locators_exceed_the_limit : given.a_metadata_query
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => Read(Enumerable.Range(0, 501).Select(_ => (ulong)_).ToArray()));

    [Fact] void should_refuse_the_request() => _error.ShouldBeOfExactType<TooManyEventLocators>();
    [Fact] void should_not_read_events() => _events.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_identities() => _identities.ReceivedCalls().ShouldBeEmpty();
}
