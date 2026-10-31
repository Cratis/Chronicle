// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverTransitionStateMerge.when_merging.given;

public class a_transition_hook : Specification
{
    protected ObserverState _handed;
    protected ObserverState _recorded;
    protected ObserverState _returned;
    protected ObserverState _result;

    void Establish()
    {
        _handed = new ObserverState();
        _recorded = _handed;
        _returned = _handed;
    }
}
