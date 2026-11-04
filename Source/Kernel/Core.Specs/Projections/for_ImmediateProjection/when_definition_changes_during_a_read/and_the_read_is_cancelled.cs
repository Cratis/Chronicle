// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_definition_changes_during_a_read;

public class and_the_read_is_cancelled : given.a_read_waiting_for_a_fold
{
    Exception _exception;

    async Task Because()
    {
        await _grain.OnProjectionDefinitionsChanged(_changed);
        _fold.SetCanceled();
        _exception = await Catch.Exception(async () => await _read);
    }

    [Fact] void should_fail_with_a_cancellation() => (_exception is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_apply_the_change_once() => _grain.AppliedDefinitions.Single().ShouldBeSame(_changed);
}
