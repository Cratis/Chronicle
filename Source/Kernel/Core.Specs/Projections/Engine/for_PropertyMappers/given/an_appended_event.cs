// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers.given;

public static class an_appended_event
{
    public static AppendedEvent with_content(ExpandoObject content) => new(
        new(
            new("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
            EventSourceType.Default,
            "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
            EventStreamType.All,
            EventStreamId.Default,
            0,
            DateTimeOffset.UtcNow,
            "123b8935-a1a4-410d-aace-e340d48f0aa0",
            "41f18595-4748-4b01-88f7-4c0d0907aa90",
            CorrelationId.New(),
            [],
            Identity.System,
            [],
            EventHash.NotSet),
        content);

    public static AppendedEvent with_slot(string? slot, object? seriesId = null)
    {
        var content = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)content;
        if (slot is not null)
        {
            dictionary["slot"] = slot;
        }

        dictionary["seriesId"] = seriesId;
        return with_content(content);
    }
}
