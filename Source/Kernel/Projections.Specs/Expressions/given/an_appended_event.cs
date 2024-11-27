// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Strings;

namespace Cratis.Chronicle.Projections.Expressions.given;

public class an_appended_event : Specification
{
    protected AppendedEvent @event;
    protected DateTimeOffset occurred;
    protected MyEvent my_event;

    void Establish()
    {
        my_event = new MyEvent(42, "Forty two");
        occurred = DateTimeOffset.UtcNow;

        var content = new ExpandoObject();
        var contentAsDictionary = content as IDictionary<string, object?>;
        contentAsDictionary.Add(nameof(MyEvent.Something).ToCamelCase(), my_event.Something);
        contentAsDictionary.Add(nameof(MyEvent.SomethingElse).ToCamelCase(), my_event.SomethingElse);

        @event = new(
            new(0,
            new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)),
            new(
                EventSourceType.Default,
                "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                EventStreamType.All,
                EventStreamId.Default,
                0,
                occurred,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System),
            content);
    }
}
