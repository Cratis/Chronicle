// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Integration.for_ImportBuilderExtensions;

public class when_filtering_on_model_properties_with_similar_name_are_set : given.a_change_on_similarily_named_property
{
    ImportContext<Model, ExternalModel> result;

    void Establish()
    {
        context = import_builder.WhenModelPropertiesAreSet(_ => _.SomeString);
        context.Subscribe(_ => result = _);
    }

    void Because() =>
        subject.OnNext(
            new ImportContext<Model, ExternalModel>(
                new AdapterProjectionResult<Model>(
                    new(0, default, default),
                    new PropertyPath[] { new(nameof(Model.SomeString2)) },
                    1),
                changeset,
                events_to_append));

    [Fact]
    void should_filter_through_the_context() => result.ShouldBeNull();
}
