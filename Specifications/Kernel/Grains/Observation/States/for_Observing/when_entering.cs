// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.EventSequences;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Observing;

public class when_entering : given.an_observing_state
{
    void Establish() => stored_state = stored_state with { NextEventSequenceNumber = 42UL };

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_subscribe_to_stream() => stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), new EventSequenceNumberToken(stored_state.NextEventSequenceNumber), null), Once());
}
