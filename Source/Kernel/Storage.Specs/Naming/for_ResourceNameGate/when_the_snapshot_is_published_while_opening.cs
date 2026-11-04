// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_the_snapshot_is_published_while_opening : given.a_gate
{
    const int Publications = 200;
    ResourceNameStatus[] _seen;

    void Establish() => _snapshot = ResourceNameSnapshot.NotReady();

    async Task Because()
    {
        using var start = new ManualResetEventSlim();
        var checker = Task.Run(() =>
        {
            start.Wait();
            var seen = new HashSet<ResourceNameStatus>();
            for (var i = 0; i < 50_000; i++)
            {
                seen.Add(_snapshot.Check("orders"));
                seen.Add(_snapshot.Check("orders", "tenant"));
            }

            return seen;
        });
        var publisher = Task.Run(() =>
        {
            start.Wait();
            for (var i = 0; i < Publications; i++)
            {
                _snapshot.Publish([new EventStoreName("orders")], []);
            }
        });

        start.Set();
        await Task.WhenAll(checker, publisher);
        var seen = await checker;
        _seen = [.. seen];
    }

    [Fact] void should_only_see_not_ready_or_the_full_snapshot() => _seen.ShouldEachConformTo(_ => _ == ResourceNameStatus.NotReady || _ == ResourceNameStatus.Quarantined);
}
