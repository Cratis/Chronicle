// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_the_caller_deadline_expires_before_commit : given.a_namespace_transaction
{
    Exception _error;
    bool _receiptExists;

    void Establish() => _context = _context with { Deadline = _clock.GetUtcNow() + TimeSpan.FromSeconds(1) };

    async Task Because()
    {
        await _markers.TryOpen(_incarnation, null);
        _error = await Catch.Exception(() => _transaction.Execute(_context, async (database, token) =>
        {
            var result = await WriteReceipt(database, token);
            _clock.Advance(TimeSpan.FromSeconds(1));
            return result;
        }));
        _receiptExists = await HasReceipt();
    }

    [Fact] void should_report_definite_pre_commit_cancellation() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_roll_back_the_staged_receipt() => _receiptExists.ShouldBeFalse();
    [Fact] void should_not_retry() => _delays.ShouldBeEmpty();
}
