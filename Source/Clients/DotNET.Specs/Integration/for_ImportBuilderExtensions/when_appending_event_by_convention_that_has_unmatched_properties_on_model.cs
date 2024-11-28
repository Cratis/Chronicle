// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions;

public class when_appending_event_by_convention_that_has_unmatched_properties_on_model : given.changes_on_two_properties
{
    Exception result;

    void Establish() => _subject.AppendEvent<Model, ExternalModel, SomeEventWithMoreProperties>();

    void Because() =>
        result = Catch.Exception(
            () => _subject.OnNext(
                new ImportContext<Model, ExternalModel>(
                    new AdapterProjectionResult<Model>(new(0, string.Empty, string.Empty), [], 0),
                    _changeset,
                    _eventsToAppend)));

    [Fact]
    void should_throw_missing_expected_event_property_on_model() =>
        result.ShouldBeOfExactType<MissingExpectedEventPropertyOnModel>();
}
