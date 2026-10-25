// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Core.Specs.Patches.for_RemoveUnhashedUniqueConstraintValues.when_applying_up;

public class and_storage_cleanup_fails : given.a_cleanup_patch
{
    readonly InvalidOperationException _failure = new("DDL failed");
    Exception _caught;

    void Establish() => _other.RemoveUnhashedValues().Returns(Task.FromException<(long, long)>(_failure));

    async Task Because()
    {
        try
        {
            await _patch.Up();
        }
        catch (Exception exception)
        {
            _caught = exception;
        }
    }

    [Fact] void should_propagate_the_failure_so_the_patch_cannot_be_marked_complete() => _caught.ShouldEqual(_failure);
    [Fact] void should_not_continue_to_the_log_after_failure() => _log.DidNotReceive().RemoveUnhashedValues();
}
