// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_under_a_namespace_fence;

public class and_appending_an_empty_batch : given.a_fenced_append_storage
{
    async Task Because() => (await _storage.AppendMany(_context, [])).IsSuccess.ShouldBeTrue();

    [Fact] void should_still_validate_and_commit_the_namespace_fence() => _steps.SequenceEqual(["fence", "commit"]).ShouldBeTrue();
    [Fact] void should_not_issue_an_empty_insert() => _writeSessions.ShouldBeEmpty();
}
