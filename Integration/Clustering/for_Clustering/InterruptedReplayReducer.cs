// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

public class InterruptedReplayReducer : IReducerFor<InterruptedTotal>
{
    public InterruptedTotal OnNumberAdded(InterruptedNumberAdded @event, InterruptedTotal? current) =>
        new((current?.Number ?? 0) + @event.Number);
}
