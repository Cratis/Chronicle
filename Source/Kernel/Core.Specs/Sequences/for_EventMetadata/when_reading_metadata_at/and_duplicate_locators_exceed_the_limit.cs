// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventMetadata.when_reading_metadata_at;

public class and_duplicate_locators_exceed_the_limit : given.a_metadata_query
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => Read(Enumerable.Repeat(42UL, 501).ToArray()));

    [Fact] void should_enforce_the_limit_before_deduplication() => _error.ShouldBeOfExactType<TooManyEventLocators>();
    [Fact] void should_not_read_storage() => _events.ReceivedCalls().ShouldBeEmpty();
}
