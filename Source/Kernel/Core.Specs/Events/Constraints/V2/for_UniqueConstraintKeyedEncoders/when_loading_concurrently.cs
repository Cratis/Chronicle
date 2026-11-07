// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoders;

public class when_loading_concurrently : given.a_loader
{
    static readonly EventStoreName _store = "store";
    UniqueConstraintKeyedEncoder?[] _results;
    IUniqueConstraintEncodingStorage _keyStorage;

    void Establish() => _keyStorage = KeyStorageFor(_store, UniqueConstraintEncodingKey.Create());

    async Task Because()
    {
        using var gate = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            gate.Wait();
            return await _loader.TryGetFor(_store);
        })).ToArray();
        gate.Set();
        _results = await Task.WhenAll(tasks);
    }

    [Fact] void should_read_the_key_once() => _keyStorage.Received(1).GetKey();
    [Fact] void should_return_an_encoder() => _results.All(_ => _ is not null).ShouldBeTrue();
    [Fact] void should_share_one_instance() => _results.Distinct().Count().ShouldEqual(1);
}
