// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Source = Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given.AttributedSource;
using Target = Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given.AttributedTarget;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilderFor.when_resolving_names;

public class and_the_receiver_is_a_custom_builder : Specification
{
    IEventMigrationPropertyBuilder _receiver;
    EventMigrationPropertyBuilderFor<Target, Source> _builder;

    void Establish()
    {
        _receiver = Substitute.For<IEventMigrationPropertyBuilder>();
        _builder = new(_receiver);
    }

    void Because() => _builder.RenamedFrom(target => target.Contact.Email, source => source.Contact.EmailAddress);

    [Fact] void should_preserve_the_custom_builders_clr_path_contract() => _receiver.Received(1).RenamedFrom("Contact.Email", "Contact.EmailAddress");
}
