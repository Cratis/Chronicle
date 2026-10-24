// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_no_document_matches : given.a_composite_key_lookup
{
    async Task Because() => _result = await Lookup();
    [Fact] void should_not_find_an_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.NotFound);
    [Fact] void should_not_return_an_instance() => _result.Instance.ShouldBeNull();
}
