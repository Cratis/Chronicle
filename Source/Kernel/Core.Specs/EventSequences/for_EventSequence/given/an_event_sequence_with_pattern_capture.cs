// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_pattern_capture : an_event_sequence
{
    protected IObserver _captureObserver;
    protected bool _captureIsSubscribed;

    void Establish()
    {
        _captureObserver = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _captureObserver);
        _captureObserver.IsSubscribed().Returns(_ => _captureIsSubscribed);
        _patternCapture.Subscribe(EventStore, EventStoreNamespace).Returns(_ =>
        {
            _captureIsSubscribed = true;
            return Task.CompletedTask;
        });
    }
}
