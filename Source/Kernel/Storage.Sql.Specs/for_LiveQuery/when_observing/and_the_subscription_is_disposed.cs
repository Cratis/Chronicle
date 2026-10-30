// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_LiveQuery.when_observing;

public class and_the_subscription_is_disposed : Specification
{
    static readonly TimeSpan _interval = TimeSpan.FromMilliseconds(10);

    int _calls;
    int _callsAfterDispose;

    async Task Because()
    {
        var first = new TaskCompletionSource();
        var subscription = LiveQuery.Observe(
            () =>
            {
                Interlocked.Increment(ref _calls);
                return Task.FromResult<IEnumerable<int>>([1]);
            },
            _interval).Subscribe(_ => first.TrySetResult());

        await first.Task.WaitAsync(TimeSpan.FromSeconds(10));
        subscription.Dispose();
        await Task.Delay(100);
        var atDispose = Volatile.Read(ref _calls);
        await Task.Delay(200);
        _callsAfterDispose = Volatile.Read(ref _calls) - atDispose;
    }

    [Fact] void should_stop_polling() => _callsAfterDispose.ShouldEqual(0);
}
