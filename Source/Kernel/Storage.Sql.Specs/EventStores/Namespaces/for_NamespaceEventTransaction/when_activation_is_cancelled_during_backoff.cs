// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_activation_is_cancelled_during_backoff : given.a_namespace_transaction
{
    readonly CancellationTokenSource _activation = new();
    Exception _error;
    int _attempts;
    bool _receiptExists;

    void Establish()
    {
        _context = _context with { ActivationCancellation = _activation.Token };
        _duringDelay = _activation.Cancel;
    }

    async Task Because()
    {
        await _markers.TryOpen(_incarnation, null);
        _error = await Catch.Exception(() => _transaction.Execute<int>(_context, async (database, token) =>
        {
            _attempts++;
            await WriteReceipt(database, token);
            throw new PostgresException("serialization failure", "ERROR", "ERROR", "40001");
        }));
        _receiptExists = await HasReceipt();
    }

    [Fact] void should_report_cancellation() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_start_a_second_attempt() => _attempts.ShouldEqual(1);
    [Fact] void should_back_off_only_once() => _delays.Count.ShouldEqual(1);
    [Fact] void should_not_commit_the_receipt() => _receiptExists.ShouldBeFalse();

    void Destroy() => _activation.Dispose();
}
