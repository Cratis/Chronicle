// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Observation.States.for_Observing;

public class when_leaving : given.an_observing_state
{
    async Task Establish() => await state.OnEnter(stored_state);

    async Task Because() => await state.OnLeave(stored_state);

    [Fact] void should_unsubscribe_from_stream() => stream_subscription.Verify(_ => _.UnsubscribeAsync(), Once());
}
