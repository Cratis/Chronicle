// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Integration.for_ImportBuilderExtensions;

public class when_filtering_on_properties_and_next_value_with_similar_name_is_changed : given.a_change_on_similarily_named_property
{
    ImportContext<Model, ExternalModel> result;

    void Establish()
    {
        context = import_builder.WithProperties(_ => _.SomeString);
        context.Subscribe(_ => result = _);
    }

    void Because() =>
        subject.OnNext(
            new ImportContext<Model, ExternalModel>(
                new AdapterProjectionResult<Model>(new(0, default, default), Array.Empty<PropertyPath>(), 0),
                changeset,
                events_to_append));

    [Fact]
    void should_not_filter_through_the_context() => result.ShouldBeNull();
}
