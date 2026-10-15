// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.given;
using Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.when_adapting_a_plain_recipe;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_migration_recipes_fail_before_effects : given.an_explicit_store
{
    EventStore[] _stores;
    Exception[] _discoveryErrors;
    Exception[] _registrationErrors;

    void Establish()
    {
        var recipes = Enumerable.Range(0, 17).Select(and_named_roles_are_invalid.EmitRecipe).Append(typeof(UnreachableRecipe)).Append(typeof(ThrowingRecipe)).ToArray();
        _stores = recipes.Select((recipe, index) =>
        {
            _options = new ChronicleOptions { ArtifactIdentityDrift = ArtifactIdentityDriftPolicy.Ignore };
            _options.ExplicitArtifacts.RegisterEventType<PlainEvent>().RegisterMigration(recipe);
            if (index != 17)
            {
                _options.ExplicitArtifacts.RegisterEventType<a_plain_migration.Previous>(_ => _.WithId("migration").Generation(1))
                    .RegisterEventType<a_plain_migration.Upgrade>(_ => _.WithId("migration").Generation(2));
            }
            return CreateStore();
        }).ToArray();
    }

    async Task Because()
    {
        _discoveryErrors = new Exception[_stores.Length];
        _registrationErrors = new Exception[_stores.Length];
        for (var index = 0; index < _stores.Length; index++)
        {
            _discoveryErrors[index] = await Catch.Exception(_stores[index].DiscoverAll);
            _registrationErrors[index] = await Catch.Exception(_stores[index].RegisterAll);
        }
    }

    [Fact] void should_reject_every_invalid_role_during_preparation() => _discoveryErrors.Take(17).All(_ => _ is InvalidExplicitArtifact).ShouldBeTrue();
    [Fact] void should_reject_a_valid_but_unreachable_recipe() => _discoveryErrors[17].ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_preserve_a_handler_failure() => _discoveryErrors[18].ShouldBeOfExactType<RecipeFailure>();
    [Fact] void should_retain_the_exact_failed_verdicts() => _registrationErrors.Zip(_discoveryErrors).All(pair => ReferenceEquals(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_publish_the_exact_failed_outcomes() => _stores.Zip(_discoveryErrors).All(pair => ReferenceEquals(pair.First.Registration.Failure, pair.Second)).ShouldBeTrue();
    [Fact] void should_send_no_store_registration_effects() => _services.EventStores.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_send_no_event_registration_effects() => _services.EventTypes.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_send_no_read_model_registration_effects() => _services.ReadModels.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_send_no_observer_registration_effects() => _services.Reactors.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_start_background_retries() => _stores.All(_ => _.PendingBackgroundRegistrationRetry is null).ShouldBeTrue();

    public class UnreachableRecipe
    {
        public void Upcast(IEventMigrationBuilder<a_plain_migration.Upgrade, a_plain_migration.Previous> builder) { }
        public void Downcast(IEventMigrationBuilder<a_plain_migration.Previous, a_plain_migration.Upgrade> builder) { }
    }
    public class ThrowingRecipe
    {
        public void Upcast(IEventMigrationBuilder<a_plain_migration.Upgrade, a_plain_migration.Previous> builder) => throw new RecipeFailure();
        public void Downcast(IEventMigrationBuilder<a_plain_migration.Previous, a_plain_migration.Upgrade> builder) { }
    }
    public class RecipeFailure : Exception;
}
