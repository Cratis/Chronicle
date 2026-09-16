// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_rendering_nested_paths;

public class and_value_maps_target_a_custom_builder : Specification
{
    EventValueMapBuilder<given.AttributedTarget, given.AttributedSource> _maps;
    IEventMigrationPropertyBuilder _upcast;
    IEventMigrationPropertyBuilder _downcast;

    void Establish()
    {
        _upcast = Substitute.For<IEventMigrationPropertyBuilder>();
        _downcast = Substitute.For<IEventMigrationPropertyBuilder>();
        _maps = new();
        _maps.For(target => target.Contact.Status, source => source.Contact.Status, map => map.Map("old", "new"));
    }

    void Because()
    {
        _maps.ApplyUpcast(_upcast);
        _maps.ApplyDowncast(_downcast);
    }

    [Fact] void should_forward_clr_paths_and_values_for_upcast() => _upcast.Received(1).MapValues("Contact.Status", "Contact.Status", Arg.Is<IEnumerable<ValueMapping>>(_ => _.Single() == new ValueMapping("old", "new")));
    [Fact] void should_forward_clr_paths_and_inverted_values_for_downcast() => _downcast.Received(1).MapValues("Contact.Status", "Contact.Status", Arg.Is<IEnumerable<ValueMapping>>(_ => _.Single() == new ValueMapping("new", "old")));
}
