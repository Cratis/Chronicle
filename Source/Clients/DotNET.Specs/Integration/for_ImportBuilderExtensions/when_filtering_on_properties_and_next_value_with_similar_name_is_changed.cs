// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions;

public class when_filtering_on_properties_and_next_value_with_similar_name_is_changed : given.a_change_on_similarly_named_property
{
    ImportContext<Model, ExternalModel> result;

    void Establish()
    {
        _context = _importBuilder.WithProperties(_ => _.SomeString);
        _context.Subscribe(_ => result = _);
    }

    void Because() =>
        _subject.OnNext(
            new ImportContext<Model, ExternalModel>(
                new AdapterProjectionResult<Model>(new(0, default!, default!), [], 0),
                _changeset,
                _eventsToAppend));

    [Fact]
    void should_not_filter_through_the_context() => result.ShouldBeNull();
}
