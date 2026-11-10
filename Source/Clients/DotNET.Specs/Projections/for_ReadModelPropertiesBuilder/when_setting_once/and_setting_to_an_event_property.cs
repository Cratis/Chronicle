// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_setting_once;

public class and_setting_to_an_event_property : given.a_from_builder_with_dictionaries
{
    void Because()
    {
        _builder.SetOnce(m => m.Name).To(e => e.Name);
        _result = _builder.Build();
    }

    [Fact] void should_wrap_the_event_property_in_set_once() => _result.Properties["Name"].ShouldEqual("$setOnce(Name)");
}
