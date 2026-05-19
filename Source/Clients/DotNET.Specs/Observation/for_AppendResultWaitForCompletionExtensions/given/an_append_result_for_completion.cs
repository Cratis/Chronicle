// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_AppendResultWaitForCompletionExtensions.given;

public class an_append_result_for_completion : Specification
{
    protected Contracts.Observation.IObservers _observers;
    protected AppendResult _appendResult;
    protected AppendResultWaitForCompletionResult _result;

    void Establish()
    {
        _observers = Substitute.For<Contracts.Observation.IObservers>();
        _appendResult = new AppendResult
        {
            EventStore = "event-store",
            EventStoreNamespace = "event-store-namespace",
            EventSequenceId = EventSequences.EventSequenceId.Log,
            SequenceNumber = 42UL,
            Observers = _observers
        };
    }
}
