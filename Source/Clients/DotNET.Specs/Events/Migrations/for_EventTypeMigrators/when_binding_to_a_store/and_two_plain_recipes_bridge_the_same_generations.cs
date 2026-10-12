// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigrators.when_binding_to_a_store;

public class and_two_plain_recipes_bridge_the_same_generations : given.store_bindings
{
    Exception _error;

    void Establish() => _catalog = new ExplicitArtifacts()
        .RegisterEventType<Previous>(_ => _.WithId("shared"))
        .RegisterEventType<Upgrade>(_ => _.WithId("shared").Generation(2))
        .RegisterMigration<PlainMigration>()
        .RegisterMigration<OtherRecipe>()
        .CreateSnapshot().ApplyTo(_discovered);

    void Because()
    {
        _bound = StoreEventTypeMigrators.Bind(new EventTypeMigrators(_discovered, _services), _catalog, _eventTypes, _services, new DefaultNamingPolicy());
        _error = Catch.Exception(() => _bound.GetMigratorsFor(typeof(Upgrade)));
    }

    [Fact] void should_reject_the_duplicate_bridge() => _error.ShouldBeOfExactType<MultipleMigratorsForSameEventTypeGeneration>();
    [Fact] void should_name_the_first_original_recipe() => _error.Message.ShouldContain(nameof(PlainMigration));
    [Fact] void should_name_the_second_original_recipe() => _error.Message.ShouldContain(nameof(OtherRecipe));

    public class OtherRecipe
    {
        public void Upcast(IEventMigrationBuilder<Upgrade, Previous> builder)
        {
        }

        public void Downcast(IEventMigrationBuilder<Previous, Upgrade> builder)
        {
        }
    }
}
