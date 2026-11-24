// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction;

public class when_the_caller_deadline_already_expired : given.a_namespace_transaction
{
    void Establish() => _context = _context with { Deadline = _clock.GetUtcNow() };

    Task Because() => ExecuteCatching();

    [Fact] void should_refuse_before_any_transaction() => _failure.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_start_a_session() => _attempts.ShouldEqual(0);
    [Fact] void should_not_write_a_marker_or_event() => _steps.ShouldBeEmpty();
}
