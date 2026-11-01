// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.for_ChronicleClient.when_checking_deployment;

public class and_hashing_is_held_then_fails : given.a_serverless_client
{
    readonly TaskCompletionSource _hashEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseHash = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IEventTypes _live;
    bool _callerReturned;
    bool _unchanged;

    void Establish()
    {
        CreateClient();
        var field = typeof(ChronicleClient).GetField("_jsonSchemaGenerator", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var generator = new an_explicit_store.SchemaGenerator((JsonSchemaGenerator)field.GetValue(_client)!)
        {
            BeforeGenerate = (_, registry, _) =>
            {
                _live ??= registry;
                if (ReferenceEquals(registry, _live)) return;
                _hashEntered.TrySetResult();
                _releaseHash.Task.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
                throw new InvalidOperationException("diagnostic hash failure");
            }
        };
        field.SetValue(_client, generator);
    }

    async Task Because()
    {
        var store = await _client.GetEventStore("held-hash", "one").WaitAsync(TimeSpan.FromSeconds(10));
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        CompleteQuery(1, "deployed");
        await _hashEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _callerReturned = !_releaseHash.Task.IsCompleted;
        var before = store.Registration;
        _releaseHash.TrySetResult();
        await _logger.Finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _unchanged = ReferenceEquals(before, store.Registration);
    }

    void Destroy() => _releaseHash.TrySetResult();

    [Fact] void should_return_while_hashing_is_blocked() => _callerReturned.ShouldBeTrue();
    [Fact] void should_swallow_the_hash_failure() => _logger.Failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_readiness() => _unchanged.ShouldBeTrue();
}
