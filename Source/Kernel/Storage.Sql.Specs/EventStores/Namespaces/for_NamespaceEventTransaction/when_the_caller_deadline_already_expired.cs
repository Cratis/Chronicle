// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_the_caller_deadline_already_expired : given.a_namespace_transaction
{
    Exception _error;

    void Establish() => _context = _context with { Deadline = _clock.GetUtcNow() };

    async Task Because() => _error = await Catch.Exception(() => _transaction.Execute(_context, WriteReceipt));

    [Fact] void should_refuse_before_work() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_issue_receipt_work() => _called.ShouldBeFalse();
    [Fact] void should_not_consume_an_attempt() => _transaction.HasRemainingAttempts.ShouldBeTrue();
}
