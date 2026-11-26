// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeDetailsConverters.given;

public class a_registration : Specification
{
    protected EventTypeSchema _registration;

    protected EventTypeSchema RegistrationWith(EventTypeVisibility visibility, string origin = "") =>
        new(
            new EventType("some-type", new EventTypeGeneration(2)),
            EventTypeOwner.Client,
            EventTypeSource.Code,
            JsonSchema.FromJson("{\"type\":\"object\"}"),
            visibility,
            origin);
}
