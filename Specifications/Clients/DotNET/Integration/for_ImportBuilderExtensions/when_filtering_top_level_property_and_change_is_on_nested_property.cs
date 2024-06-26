// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions;

public class when_filtering_top_level_property_and_change_is_on_nested_property : given.a_change_on_a_nested_property
{
    ImportContext<ComplexModel, ExternalModel> result;

    void Establish()
    {
        context = import_builder.WithProperties(_ => _.Child);
        context.Subscribe(_ => result = _);
    }

    void Because() =>
        subject.OnNext(
            new ImportContext<ComplexModel, ExternalModel>(
                new AdapterProjectionResult<ComplexModel>(
                    new(0, string.Empty, new(0, string.Empty, string.Empty)),
                    [],
                    0),
                changeset,
                events_to_append));

    [Fact]
    void should_filter_through_the_context() => result.ShouldNotBeNull();
}
