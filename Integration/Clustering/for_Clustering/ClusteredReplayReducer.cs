// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

public class ClusteredReplayReducer(ReplayCalculation calculation) : IReducerFor<ReplayedTotal>
{
    public ReplayedTotal? OnNumberAdded(ReplayNumberAdded @event, ReplayedTotal? current) =>
        calculation.ReturnEmpty ? null : new((current?.Number ?? 0) + (@event.Number * calculation.Multiplier));
}
