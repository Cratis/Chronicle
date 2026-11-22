// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_transition_races_each_mutation : given.a_v2_store
{
    readonly List<bool> _linearized = [];
    readonly List<bool> _staleRefused = [];

    async Task Because()
    {
        for (var operation = 0; operation < 6; operation++)
        {
            _storage = new();
            _definition = _definition with { Mode = operation == 2 ? UniqueConstraintMode.PerValue : UniqueConstraintMode.PerEventSource };
            await Seed();
            using var barrier = new Barrier(3);
            var mutation = Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await Mutation(operation, _authority);
            });
            var transition = Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await _storage.TryTransition(_definition.Name, _authority, Run(1));
            });
            barrier.SignalAndWait();
            var result = await mutation;
            (await transition).IsSuccess.ShouldBeTrue();
            var values = (await _storage.GetFor(_source, _definition, _scope)).ToArray();
            UniqueConstraintValue[] expected = [_value];
            if (result.IsSuccess)
            {
                expected = operation == 0 ? [_otherValue] : [];
            }

            _linearized.Add(values.SequenceEqual(expected) && (result.IsSuccess || result.AsT1 == UniqueConstraintWriteFailure.AuthorityMismatch));
            var resumed = await Mutation(operation, _authority);
            _staleRefused.Add(!resumed.IsSuccess && resumed.AsT1 == UniqueConstraintWriteFailure.AuthorityMismatch && (await _storage.GetFor(_source, _definition, _scope)).SequenceEqual(values));
        }
    }

    [Fact] void should_linearize_all_six_mutations_before_or_after_the_transition() => _linearized.ShouldEqual(Enumerable.Repeat(true, 6));
    [Fact] void should_refuse_every_resumed_old_proof_without_mutation() => _staleRefused.ShouldEqual(Enumerable.Repeat(true, 6));
}
