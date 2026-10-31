// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_EventSourceIdTagging;

public class when_creating_without_a_required_hash_key : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _ = new EventSourceIdTagging(new() { EventSourceIdTracing = EventSourceIdTracing.Hashed }));

    [Fact] void should_fail_before_any_append() => _exception.ShouldBeOfExactType<InvalidIdentifierHashKey>();
}
