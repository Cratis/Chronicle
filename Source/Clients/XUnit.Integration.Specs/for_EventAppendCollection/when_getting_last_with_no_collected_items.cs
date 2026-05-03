// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_getting_last_with_no_collected_items : given.an_event_append_collection
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _ = _collection.Last);

    [Fact] void should_throw() => _exception.ShouldBeOfExactType<InvalidOperationException>();
}
