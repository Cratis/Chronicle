// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_setting_once;

public class and_the_property_is_already_set : given.a_from_builder_with_dictionaries
{
    Exception? _error;

    void Because()
    {
        _builder.Set(m => m.Name).To(e => e.Name);
        _error = Catch.Exception(() => _builder.SetOnce(m => m.Name));
    }

    [Fact] void should_fail_with_duplicate_property() => _error.ShouldBeOfExactType<DuplicatePropertyInProjection>();
}
