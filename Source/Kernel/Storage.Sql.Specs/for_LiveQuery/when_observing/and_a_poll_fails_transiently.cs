// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.for_LiveQuery.when_observing;

public class and_a_poll_fails_transiently : Specification
{
    static readonly TimeSpan _interval = TimeSpan.FromMilliseconds(10);

    int _calls;
    Exception? _error;
    IEnumerable<int>? _received;
    IDisposable _subscription;

    async Task Because()
    {
        var received = new TaskCompletionSource<IEnumerable<int>>();
        _subscription = LiveQuery.Observe(
            () => ++_calls == 1 ? throw new SqliteException("database is locked", 5) : Task.FromResult<IEnumerable<int>>([1, 2]),
            _interval).Subscribe(
                items => received.TrySetResult(items),
                error =>
                {
                    _error = error;
                    received.TrySetException(error);
                });

        _received = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_deliver_the_data_from_the_next_poll() => _received.ShouldContainOnly(1, 2);
    [Fact] void should_not_report_an_error() => _error.ShouldBeNull();

    void Destroy() => _subscription?.Dispose();
}
