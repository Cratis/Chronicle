// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Core.Specs.Patches.for_RemoveUnhashedUniqueConstraintValues.when_applying_up;

public class and_all_namespaces_are_discovered : given.a_cleanup_patch
{
    void Establish() => _log.RemoveUnhashedValues().Returns(Task.FromResult((3L, 5L)));

    Task Because() => _patch.Up();

    [Fact] void should_clean_the_log_even_if_not_listed() => _log.Received(4).RemoveUnhashedValues();
    [Fact] void should_clean_each_other_sequence_once_in_each_store_and_namespace() => _other.Received(4).RemoveUnhashedValues();
    [Fact] void should_log_the_deleted_counts() => _logger.Messages.Count(message => message.Contains("3 per-event-source and 5 per-value")).ShouldEqual(4);
    [Fact] void should_log_every_cleaned_sequence() => _logger.Messages.Count(message => message.StartsWith("Removed ")).ShouldEqual(8);
    [Fact] void should_run_after_the_latest_released_version() => (_patch.Version > new Concepts.System.SemanticVersion(19, 110, 1)).ShouldBeTrue();
}
