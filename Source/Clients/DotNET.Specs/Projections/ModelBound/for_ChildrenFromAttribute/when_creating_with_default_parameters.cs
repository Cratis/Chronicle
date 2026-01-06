// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

namespace Cratis.Chronicle.Projections.ModelBound.for_ChildrenFromAttribute;

public class when_creating_with_default_parameters : Specification
{
    ChildrenFromAttribute<SomeEvent> _attribute;

    void Because() => _attribute = new ChildrenFromAttribute<SomeEvent>();

    [Fact] void should_have_auto_map_set_to_enabled() => _attribute.AutoMap.ShouldEqual(AutoMap.Enabled);
}

public record SomeEvent(Guid ItemId, string Name);
