// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_IEventSequenceMutationRegistry;

public class when_using_the_unsupported_registry : given.a_registry_contract
{
    Exception[] _errors;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        _errors =
        [
            await Catch.Exception(async () => await UnsupportedEventSequenceMutationRegistry.Instance.Begin(_request, _target, cancellation.Token)),
            await Catch.Exception(async () => await UnsupportedEventSequenceMutationRegistry.Instance.Transition(_identity, _token, EventSequenceMutationTransition.BeginApplying, cancellation.Token)),
            await Catch.Exception(async () => await UnsupportedEventSequenceMutationRegistry.Instance.Archive(_identity, _token, cancellation.Token)),
            await Catch.Exception(async () => await UnsupportedEventSequenceMutationRegistry.Instance.BeginTracking(_identity, EventSequenceMutationCoverage.Untracked, cancellation.Token))
        ];
    }

    [Fact] void should_throw_the_typed_not_supported_error_for_all_operations() => _errors.All(_ => _.GetType() == typeof(EventSequenceMutationRegistryNotSupported)).ShouldBeTrue();
    [Fact] void should_identify_all_four_unsupported_operations() => _errors.Cast<EventSequenceMutationRegistryNotSupported>().Select(_ => _.Operation).ShouldContainOnly(["Begin", "Transition", "Archive", "BeginTracking"]);
}
