// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_under_a_namespace_fence;

public class and_the_marker_has_been_closed : given.a_fenced_append_storage
{
    void Establish() => _matches = 0;
    async Task Because() => _failure = await Catch.Exception(() => _storage.Append(_context, _event));

    [Fact] void should_report_the_closed_or_replaced_identity() => _failure.ShouldBeOfExactType<NamespaceCommitRejected>();
    [Fact] void should_abort_without_inserting() => _steps.SequenceEqual(["fence", "abort"]).ShouldBeTrue();
    [Fact] void should_not_write_any_events() => _inserted.ShouldBeEmpty();
}
