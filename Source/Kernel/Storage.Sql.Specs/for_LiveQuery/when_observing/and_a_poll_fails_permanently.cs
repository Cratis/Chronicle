// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_LiveQuery.when_observing;

public class and_a_poll_fails_permanently : Specification
{
    static readonly TimeSpan _interval = TimeSpan.FromMilliseconds(10);

    Exception? _error;
    IDisposable _subscription;

    async Task Because()
    {
        var failed = new TaskCompletionSource<Exception>();
        _subscription = LiveQuery.Observe<int>(
            () => throw new InvalidOperationException("broken"),
            _interval).Subscribe(_ => { }, error => failed.TrySetResult(error));

        _error = await failed.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_report_the_error() => _error.ShouldBeOfExactType<InvalidOperationException>();

    void Destroy() => _subscription?.Dispose();
}
