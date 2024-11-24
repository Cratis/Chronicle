// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions;

public class when_filtering_on_model_properties_are_set_and_they_are : given.a_change_on_one_property
{
    ImportContext<Model, ExternalModel> result;

    void Establish()
    {
        _context = _importBuilder.WhenModelPropertiesAreSet(_ => _.SomeString);
        _context.Subscribe(_ => result = _);
    }

    void Because() =>
        _subject.OnNext(
            new ImportContext<Model, ExternalModel>(
                new AdapterProjectionResult<Model>(new(0, string.Empty, string.Empty), [new(nameof(Model.SomeString))], 1),
                _changeset,
                _eventsToAppend));

    [Fact]
    void should_filter_through_the_context() => result.ShouldNotBeNull();
}
