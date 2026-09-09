// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_reading_artifacts_during_discovery : given.an_artifacts_provider
{
    readonly TaskCompletionSource _discoveryBlocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _readerStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ManualResetEventSlim _releaseDiscovery = new();
    IEnumerable<Type>[] _firstArtifacts;
    IEnumerable<Type>[] _secondArtifacts;
    Exception _blockedReadError;
    int _definitionReads;

    void Establish() => _assembliesProvider.DefinedTypes.Returns(_ =>
    {
        // Event types have been populated, but constraints and migrators have not.
        if (++_definitionReads == 2)
        {
            _discoveryBlocked.SetResult();
            _releaseDiscovery.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
        }
        return _definedTypes;
    });

    async Task Because()
    {
        // Dedicated threads keep a blocked discovery from starving the second reader.
        var firstReader = Task.Factory.StartNew(ReadArtifacts, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        Task<IEnumerable<Type>[]> secondReader = null;
        try
        {
            await _discoveryBlocked.Task.WaitAsync(TimeSpan.FromSeconds(10));
            secondReader = Task.Factory.StartNew(
                () =>
                {
                    _readerStarted.SetResult();
                    return ReadArtifacts();
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            await _readerStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // This deadline observes non-completion while discovery is held by our signal;
            // it does not sleep to guess when discovery has finished.
            _blockedReadError = await Catch.Exception(async () => await secondReader.WaitAsync(TimeSpan.FromMilliseconds(100)));
        }
        finally
        {
            _releaseDiscovery.Set();
            _firstArtifacts = await firstReader.WaitAsync(TimeSpan.FromSeconds(10));
            if (secondReader is not null) _secondArtifacts = await secondReader.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    [Fact] void should_not_return_while_discovery_is_blocked() => _blockedReadError.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_return_the_same_complete_collections_to_both_readers() => _firstArtifacts.Zip(_secondArtifacts).All(_ => ReferenceEquals(_.First, _.Second)).ShouldBeTrue();
    [Fact] void should_return_discovered_event_types() => _secondArtifacts[0].ShouldContainOnly(typeof(ClientArtifactDiscovered));
    [Fact] void should_return_constraints_discovered_after_the_block() => _secondArtifacts[11].ShouldContainOnly(typeof(ClientArtifactDiscovered));
    [Fact] void should_return_the_last_discovered_collection() => _secondArtifacts[^1].ShouldContainOnly(typeof(EventTypeMigration<,>));
    [Fact] void should_initialize_only_once() => _assembliesProvider.Received(1).Initialize();

    void Destroy() => _releaseDiscovery.Dispose();
}
