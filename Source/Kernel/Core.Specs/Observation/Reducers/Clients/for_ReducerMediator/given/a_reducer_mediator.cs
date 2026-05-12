// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerMediator.given;

public class a_reducer_mediator : Specification
{
    protected ReducerMediator _mediator;

    void Establish() => _mediator = new ReducerMediator();
}