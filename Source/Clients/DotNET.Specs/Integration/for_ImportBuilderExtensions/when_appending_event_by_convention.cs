// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions;

public class when_appending_event_by_convention : given.changes_on_two_properties
{
    void Establish() => _subject.AppendEvent<Model, ExternalModel, SomeEvent>();

    void Because() =>
        _subject.OnNext(
            new ImportContext<Model, ExternalModel>(
                new AdapterProjectionResult<Model>(new(0, string.Empty, string.Empty), [], 0),
                _changeset,
                _eventsToAppend));

    [Fact]
    void should_append_correct_event_type() => _eventsToAppend.First().ShouldBeOfExactType<SomeEvent>();

    [Fact]
    void should_automatically_map_string_property() =>
        ((SomeEvent)_eventsToAppend.First()).SomeString.ShouldEqual(_modifiedModel.SomeString);

    [Fact]
    void should_automatically_map_integer_property() =>
        ((SomeEvent)_eventsToAppend.First()).SomeInteger.ShouldEqual(_modifiedModel.SomeInteger);
}
