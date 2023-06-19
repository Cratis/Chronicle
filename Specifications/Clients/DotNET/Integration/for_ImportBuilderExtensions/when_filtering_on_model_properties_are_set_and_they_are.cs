// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Integration.for_ImportBuilderExtensions;

public class when_filtering_on_model_properties_are_set_and_they_are : given.a_change_on_one_property
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
                new AdapterProjectionResult<Model>(new(0, string.Empty, string.Empty), new PropertyPath[] { new(nameof(Model.SomeString)) }, 1),
                changeset,
                events_to_append));

    [Fact]
    void should_filter_through_the_context() => result.ShouldNotBeNull();
}
