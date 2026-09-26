// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_an_empty_append_batch_is_observed : given.an_event_append_collection
{
    Exception _error;

    void Because() => _error = Record.Exception(() => _subject.OnNext([]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_not_collect_events() => _collection.All.ShouldBeEmpty();
}
