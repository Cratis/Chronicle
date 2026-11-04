// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.given;

public class a_reducer_dispatcher : all_dependencies
{
    internal ReducerDispatcher _dispatcher;

    void Establish() => _dispatcher = new(
        _eventStore,
        _serviceProvider,
        _artifactActivator,
        _eventTypes,
        _jsonSerializerOptions,
        _identityProvider,
        _activitySource,
        _logger);
}
