// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.EventSequences;
using Aksio.Strings;

namespace Aksio.Cratis.Kernel.Engines.Projections.Expressions.given;

public record MyEvent(int Something, string SomethingElse);

public class an_appended_event : Specification
{
    protected AppendedEvent @event;
    protected DateTimeOffset occurred;
    protected MyEvent MyEvent;

    void Establish()
    {
        MyEvent = new MyEvent(42, "Forty two");
        occurred = DateTimeOffset.UtcNow;

        var content = new ExpandoObject();
        var contentAsDictionary = content as IDictionary<string, object?>;
        contentAsDictionary.Add(nameof(given.MyEvent.Something).ToCamelCase(), MyEvent.Something);
        contentAsDictionary.Add(nameof(given.MyEvent.SomethingElse).ToCamelCase(), MyEvent.SomethingElse);

        @event = new(
            new(0,
            new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)),
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", 0, occurred, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", "50308963-d8b5-4b6e-97c7-e2486e8237e1", "bfb7fd4a-1822-4937-a6d1-52464a173f84"),
            content);
    }
}
