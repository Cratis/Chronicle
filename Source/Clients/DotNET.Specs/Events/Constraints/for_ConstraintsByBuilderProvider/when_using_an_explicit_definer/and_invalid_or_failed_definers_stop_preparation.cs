// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintsByBuilderProvider.when_using_an_explicit_definer;

public class and_invalid_or_failed_definers_stop_preparation : an_explicit_store
{
    EventStore[] _stores;
    Exception[] _discoveryErrors;
    Exception[] _registrationErrors;
    Exception _definitionFailure;
    Exception _activationFailure;

    void Establish()
    {
        _definitionFailure = new Exception("definition failure");
        _activationFailure = new ClientArtifactActivationFailed(typeof(PlainDefiner), new Exception("activation failure"));
        var invalid = Enumerable.Range(0, 11).Select(EmitDefiner).ToArray();
        var activationType = EmitDefiner(11);
        foreach (var type in invalid) _activator.ActivateNonDisposable(type).Returns(Cratis.Monads.Catch<object>.Success(Activator.CreateInstance(type)!));
        _activator.ActivateNonDisposable(typeof(PlainDefiner)).Returns(new PlainDefiner(_definitionFailure));
        _activator.ActivateNonDisposable(activationType).Returns(call => _activationFailure);
        _stores = invalid.Append(typeof(PlainDefiner)).Append(activationType).Select(type =>
        {
            _options = new ChronicleOptions { ArtifactIdentityDrift = ArtifactIdentityDriftPolicy.Ignore };
            _options.ExplicitArtifacts.RegisterEventType<PlainEvent>().RegisterConstraint(type);
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

    [Fact] void should_reject_missing_non_public_static_async_optional_byref_generic_overloaded_wrong_or_params_roles() => _discoveryErrors.Take(11).All(_ => _ is InvalidExplicitArtifact).ShouldBeTrue();
    [Fact] void should_preserve_the_exact_definition_failure() => _discoveryErrors[11].ShouldBeSame(_definitionFailure);
    [Fact] void should_preserve_the_activation_failure() => _discoveryErrors[12].ShouldBeSame(_activationFailure);
    [Fact] void should_retain_every_failed_verdict_for_registration() => _discoveryErrors.Zip(_registrationErrors).All(pair => ReferenceEquals(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_send_no_store_registration_effects() => _services.EventStores.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_send_no_event_registration_effects() => _services.EventTypes.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_send_no_constraint_registration_effects() => _services.Constraints.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_start_background_retries() => _stores.All(_ => _.PendingBackgroundRegistrationRetry is null).ShouldBeTrue();

    static Type EmitDefiner(int shape)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("Definers").DefineType("PlainDefiner", TypeAttributes.Public);
        type.DefineDefaultConstructor(MethodAttributes.Public);
        if (shape != 0)
        {
            var parameter = shape switch
            {
                6 => typeof(IConstraintBuilder).MakeByRefType(),
                9 => typeof(object),
                10 => typeof(IConstraintBuilder[]),
                _ => typeof(IConstraintBuilder)
            };
            var flags = shape switch
            {
                1 => MethodAttributes.Private,
                2 => MethodAttributes.Public | MethodAttributes.Static,
                _ => MethodAttributes.Public
            };
            var role = type.DefineMethod("Define", flags, shape == 3 ? typeof(Task) : typeof(void), [parameter]);
            if (shape == 3) role.GetILGenerator().Emit(OpCodes.Ldnull);
            role.GetILGenerator().Emit(OpCodes.Ret);
            if (shape == 4) role.SetCustomAttribute(new CustomAttributeBuilder(typeof(AsyncStateMachineAttribute).GetConstructor([typeof(Type)])!, [typeof(object)]));
            if (shape == 5) role.DefineParameter(1, ParameterAttributes.Optional, "builder");
            if (shape == 7) role.DefineGenericParameters("T");
            if (shape == 8) type.DefineMethod("Define", MethodAttributes.Public, typeof(void), [typeof(object)]).GetILGenerator().Emit(OpCodes.Ret);
            if (shape == 10) role.DefineParameter(1, ParameterAttributes.None, "builders").SetCustomAttribute(new CustomAttributeBuilder(typeof(ParamArrayAttribute).GetConstructor([])!, []));
        }
        return type.CreateType();
    }

    public class PlainDefiner(Exception failure)
    {
        public void Define(IConstraintBuilder builder) => throw failure;
    }
}
