// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_getting_all_instances;

public class and_erasure_is_confirmed_while_reading_a_later_page : given.a_materialized_read_model
{
    readonly TaskCompletionSource _paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ReadModelInstances> _laterPage = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ProtectedValues.for_ReleaseKeys.given.a_release_clock _clock = new();
    IEncryptionKeyStorage _keys;
    EncryptionKeyAtRevision _pair;
    Exception _error;

    void Establish()
    {
        _keys = Substitute.For<IEncryptionKeyStorage>();
        _pair = new(new([1], [2]), 999u, 1);
        var first = InstanceNamed("plaintext");
        _sink.GetInstances(null, 0, 100).Returns(new ReadModelInstances([first], 2));
        _sink.GetInstances(null, 1, 100).Returns(_ =>
        {
            _paused.SetResult();
            return _laterPage.Task;
        });
        _compliance.Release(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), Arg.Any<IEnumerable<ExpandoObject>>())
            .Returns(call => ReleasePage(call.ArgAt<IEnumerable<ExpandoObject>>(3)));
    }

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        var read = _store.GetAllInstances(EventStore, EventStoreNamespace, _definition);
        await _paused.Task;
        _keys.GetErasureForRelease(EventStore, EventStoreNamespace, "subject").Returns(new EncryptionKeyErasure(1u, [], false, 2, ErasedLifecycle: 1));
        _clock.Advance(TimeSpan.FromSeconds(6));
        _laterPage.SetResult(new ReadModelInstances([], 2));
        _error = await Catch.Exception(async () => await read);
    }

    [Fact] void should_not_release_plaintext_from_the_earlier_page() => _error.ShouldBeOfExactType<EncryptionKeyErased>();

    async Task<IEnumerable<ExpandoObject>> ReleasePage(IEnumerable<ExpandoObject> instances)
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        ReleaseKeys.Track(_keys, (EventStore, EventStoreNamespace, "subject"), _pair);
        return await ReleaseKeys.Complete(() => instances);
    }
}
