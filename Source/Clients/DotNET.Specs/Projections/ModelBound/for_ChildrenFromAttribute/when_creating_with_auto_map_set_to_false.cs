// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ChildrenFromAttribute;

public class when_creating_with_auto_map_set_to_false : Specification
{
    ChildrenFromAttribute<SomeEvent> _attribute;

    void Because() => _attribute = new ChildrenFromAttribute<SomeEvent>(autoMap: AutoMap.Disabled);

    [Fact] void should_have_auto_map_set_to_disabled() => _attribute.AutoMap.ShouldEqual(AutoMap.Disabled);
}
