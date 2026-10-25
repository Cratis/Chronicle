// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_resolving_before_registration.given;

public class an_event_types_without_registration : Specification
{
    protected EventTypes _eventTypes;
    protected IStorage _storage;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(AlertRaised), typeof(AlertEscalated), typeof(AlertCleared)]);
        _storage = Substitute.For<IStorage>();
        _eventTypes = new(types, _storage, NullLogger<EventTypes>.Instance);
    }
}
