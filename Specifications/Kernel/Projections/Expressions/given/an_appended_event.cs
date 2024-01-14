// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;
using Aksio.Strings;

namespace Aksio.Cratis.Kernel.Projections.Expressions.given;

public record MyEvent(int Something, string SomethingElse);

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
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", 0, occurred, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", Enumerable.Empty<Causation>(), Identity.System),
            content);
    }
}
