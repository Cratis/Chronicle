// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_disposed : given.an_event_append_collection
{
    void Because() => _collection.Dispose();

    [Fact] void should_unsubscribe_from_the_event_sequence() => (_eventLog as IObservableEventSequence)!.Received(1).Unsubscribe(_collection);
}
