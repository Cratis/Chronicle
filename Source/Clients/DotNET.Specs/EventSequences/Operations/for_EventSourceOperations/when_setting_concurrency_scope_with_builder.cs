// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences.Operations.given;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSourceOperations;

public class when_setting_concurrency_scope_with_builder : a_new_event_source_operations
{
    ConcurrencyScope _concurrencyScope;

    void Establish()
    {
        _concurrencyScope = new ConcurrencyScope(
            42,
            Guid.NewGuid(),
            "My Type",
            "Some stream identifier",
            "Some event source type",
            ["1", "2"]);
    }

    void Because() => _operations.WithConcurrencyScope(_concurrencyScope);

    [Fact] void should_set_concurrency_scope() => _operations.ConcurrencyScope.ShouldEqual(_concurrencyScope);
}
