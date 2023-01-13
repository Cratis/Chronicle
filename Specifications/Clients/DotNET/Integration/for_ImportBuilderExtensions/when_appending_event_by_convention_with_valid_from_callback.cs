// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Integration.for_ImportBuilderExtensions;

public class when_appending_event_by_convention_with_valid_from_callback : given.changes_on_two_properties
{
    DateTimeOffset valid_from;

    void Establish()
    {
        valid_from = DateTimeOffset.UtcNow.AddDays(Random.Shared.Next(7));
        subject.AppendEvent<Model, ExternalModel, SomeEvent>(_ => valid_from);
    }

    void Because() =>
        subject.OnNext(
            new ImportContext<Model, ExternalModel>(
                new AdapterProjectionResult<Model>(new(0, string.Empty, string.Empty), Array.Empty<PropertyPath>(), 0),
                changeset,
                events_to_append));

    [Fact] void should_append_correct_event_type() => events_to_append.First().Event.ShouldBeOfExactType<SomeEvent>();
    [Fact] void should_automatically_map_string_property() => ((SomeEvent)events_to_append.First().Event).SomeString.ShouldEqual(modified_model.SomeString);
    [Fact] void should_automatically_map_integer_property() => ((SomeEvent)events_to_append.First().Event).SomeInteger.ShouldEqual(modified_model.SomeInteger);
    [Fact] void should_include_valid_from() => events_to_append.First().ValidFrom.ShouldEqual(valid_from);
}
