// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorHandler.given;

public class a_reactor_handler : all_dependencies
{
    protected ReactorHandler handler;

    void Establish() => handler = new(
        _eventStore,
        _reactorId,
        typeof(object),
        _eventSequenceId,
        [],
        _causationManager,
        _identityProvider);
}
