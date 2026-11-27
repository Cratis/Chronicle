// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_definite_conflicts_exhaust_the_shared_budget : given.a_namespace_transaction
{
    readonly PostgresException _conflict = new("serialization failure", "ERROR", "ERROR", "40001");
    Exception _error;
    int _attempts;
    bool _receiptExists;

    async Task Because()
    {
        await _markers.TryOpen(_incarnation, null);
        _error = await Catch.Exception(() => _transaction.Execute<int>(_context, async (database, token) =>
        {
            _attempts++;
            await WriteReceipt(database, token);
            throw _conflict;
        }));
        _receiptExists = await HasReceipt();
    }

    [Fact] void should_allow_only_three_attempts() => _attempts.ShouldEqual(3);
    [Fact] void should_preserve_the_definite_abort_failure() => _error.ShouldBeSame(_conflict);
    [Fact] void should_apply_backoff_to_only_the_two_retries() => _delays.ShouldEqual<TimeSpan>([TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100)]);
    [Fact] void should_roll_back_all_failed_receipts() => _receiptExists.ShouldBeFalse();
}
