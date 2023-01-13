// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Integration.for_ImportBuilderExtensions;

public class when_appending_event_from_callback_with_valid_from_callback : given.no_changes
{
    const string event_to_append = "Forty Two";
    DateTimeOffset valid_from;

    void Establish()
    {
        valid_from = DateTimeOffset.UtcNow.AddDays(Random.Shared.Next(7));
        subject.AppendEvent(_ => event_to_append, _ => valid_from);
    }

    void Because() =>
        subject.OnNext(
            new ImportContext<Model, ExternalModel>(
                new AdapterProjectionResult<Model>(new(0, string.Empty, string.Empty), Array.Empty<PropertyPath>(), 0),
                changeset,
                events_to_append));

    [Fact] void should_append_event() => events_to_append.First().Event.ShouldEqual(event_to_append);
    [Fact] void should_include_valid_from() => events_to_append.First().ValidFrom.ShouldEqual(valid_from);
}
