// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_a_non_generic_reducer_is_nominated_as_a_reactor : given.an_explicit_store
{
    Type _type;
    Exception _catalogFailure;
    Exception _runtimeFailure;
    Exception _registrationFailure;
    int _schemas;

    void Establish()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"ReducerKind{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var builder = assembly.DefineDynamicModule("Artifacts").DefineType("NonGenericReducer", TypeAttributes.Public);
        builder.AddInterfaceImplementation(typeof(IReducer));
        _type = builder.CreateType();
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>().RegisterReactor(_type);
        _generator.BeforeGenerate = (_, _, _) => _schemas++;
        _store = CreateStore();
    }

    async Task Because()
    {
        _runtimeFailure = await Catch.Exception(() => _store.Reactors.Register(_type));
        _catalogFailure = await Catch.Exception(_store.DiscoverAll);
        _registrationFailure = await Catch.Exception(_store.RegisterAll);
    }

    [Fact] void should_reject_the_catalog_nomination_as_a_kind_conflict() => _catalogFailure.ShouldBeOfExactType<ConflictingArtifactKind>();
    [Fact] void should_reject_the_runtime_nomination_as_the_same_kind_conflict() => _runtimeFailure.ShouldBeOfExactType<ConflictingArtifactKind>();
    [Fact] void should_retain_the_catalog_failure_for_registration() => ReferenceEquals(_registrationFailure, _catalogFailure).ShouldBeTrue();
    [Fact] void should_publish_the_exact_catalog_failure() => ReferenceEquals(_store.Registration.Failure, _catalogFailure).ShouldBeTrue();
    [Fact] void should_fail_before_schema_generation() => _schemas.ShouldEqual(0);
    [Fact] void should_not_ensure_the_store() => _services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
    [Fact] void should_not_register_events() => _services.EventTypes.DidNotReceive().RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>());
    [Fact] void should_not_activate_any_artifact() => _activator.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_start_a_background_retry() => _store.PendingBackgroundRegistrationRetry.ShouldBeNull();
}
