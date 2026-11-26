// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Slices.for_ProjectionSlice;

public class a_projection_slice : Specification
{
    protected ReadModelScenario<Tally> _scenario;
    protected ProjectionSlice<Tally> _slice;
    protected EventSourceId _source;
    Defaults _defaults;

    void Establish()
    {
        _source = "tally";
        _defaults = new(new InMemoryClientArtifactsProvider { EventTypes = [typeof(Tallied), typeof(TallyBroke), typeof(SequenceProbed)], Reducers = [typeof(TallyReducer)] });
        _slice = new(_defaults);
        _scenario = _slice.Scenario;
    }

    void Destroy()
    {
        _slice.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }
}
