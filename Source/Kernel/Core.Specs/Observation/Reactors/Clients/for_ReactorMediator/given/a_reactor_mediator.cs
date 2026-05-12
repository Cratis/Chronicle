// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorMediator.given;

public class a_reactor_mediator : Specification
{
    protected ReactorMediator _mediator;

    void Establish() => _mediator = new ReactorMediator();
}