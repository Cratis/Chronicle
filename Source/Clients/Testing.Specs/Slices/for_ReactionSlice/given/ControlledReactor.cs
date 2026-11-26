// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Slices.for_ReactionSlice.given;

public class ControlledReactor(ReactorControl control) : IReactor
{
    public async Task Handle(ReactorRouteRecorded @event)
    {
        control.Entered.TrySetResult();
        await control.Release.Task;
        if (control.Fail) throw new ControlledFailure();
        control.Inputs.Add(@event.Note);
    }
}
