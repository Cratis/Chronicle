// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_IEventSequenceMutationRegistry;

public class when_inspecting_the_contract : Specification
{
    MethodInfo[] _methods;

    void Because() => _methods = typeof(IEventSequenceMutationRegistry).GetMethods();

    [Fact] void should_expose_exactly_four_operations() => _methods.Select(_ => _.Name).ShouldContainOnly(["Begin", "Transition", "Archive", "BeginTracking"]);
    [Fact] void should_not_expose_a_seal_operation() => _methods.Any(_ => _.Name.Contains("Seal", StringComparison.Ordinal)).ShouldBeFalse();

    [Fact]
    void should_expose_the_exact_begin_contract() =>
        AssertMethod<EventSequenceMutationBeginResult>(
            nameof(IEventSequenceMutationRegistry.Begin),
            typeof(EventSequenceMutationRequest),
            typeof(EventSequenceMutationTarget),
            typeof(CancellationToken));

    [Fact]
    void should_expose_the_exact_transition_contract() =>
        AssertMethod<EventSequenceMutationRegistryTransitionResult>(
            nameof(IEventSequenceMutationRegistry.Transition),
            typeof(EventSequenceMutationIdentity),
            typeof(EventSequenceMutationStateToken),
            typeof(EventSequenceMutationTransition),
            typeof(CancellationToken));

    [Fact]
    void should_expose_the_exact_archive_contract() =>
        AssertMethod<EventSequenceMutationRegistryArchiveResult>(
            nameof(IEventSequenceMutationRegistry.Archive),
            typeof(EventSequenceMutationIdentity),
            typeof(EventSequenceMutationStateToken),
            typeof(CancellationToken));

    [Fact]
    void should_expose_the_exact_begin_tracking_contract() =>
        AssertMethod<EventSequenceMutationTrackingResult>(
            nameof(IEventSequenceMutationRegistry.BeginTracking),
            typeof(EventSequenceMutationIdentity),
            typeof(EventSequenceMutationCoverage),
            typeof(CancellationToken));

    void AssertMethod<TResult>(string name, params Type[] parameterTypes)
    {
        var method = typeof(IEventSequenceMutationRegistry).GetMethod(name, parameterTypes);
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldEqual(typeof(Task<TResult>));
        var cancellation = method.GetParameters()[^1];
        cancellation.Name.ShouldEqual("cancellationToken");
        cancellation.IsOptional.ShouldBeTrue();
        cancellation.HasDefaultValue.ShouldBeTrue();
    }
}
