// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventTypes;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_ReactorsReactor.given;

public class a_reactors_reactor : Specification
{
    protected ReactorsReactor _reactor;
    protected IReactors _reactors;
    protected IEventTypes _eventTypes;

    void Establish()
    {
        _reactors = Substitute.For<IReactors>();
        _eventTypes = Substitute.For<IEventTypes>();
        _reactor = new ReactorsReactor(_reactors, _eventTypes);
    }
}
