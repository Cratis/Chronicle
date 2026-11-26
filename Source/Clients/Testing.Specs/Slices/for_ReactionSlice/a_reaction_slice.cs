// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

public class a_reaction_slice<TReactor> : Specification
    where TReactor : class
{
    protected ReactorScenario<TReactor> _scenario;
    protected ReactionSlice<TReactor> _slice;
    protected EventSourceId _source;
    protected Defaults _defaults;

    void Establish()
    {
        _source = "reactor-input";
        _defaults = new(new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(ReactorRouteRecorded), typeof(AssertionEvent), typeof(ProducedRouteRecorded), typeof(VibeStarted)],
            Reactors = [typeof(TReactor)]
        });
        _slice = new(_defaults);
        _scenario = _slice.Scenario;
    }

    void Destroy()
    {
        _slice.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }
}
