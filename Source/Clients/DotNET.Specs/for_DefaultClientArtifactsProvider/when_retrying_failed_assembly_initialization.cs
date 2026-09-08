// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_retrying_failed_assembly_initialization : given.an_artifacts_provider
{
    Exception _failure;
    Exception _error;
    IEnumerable<Type> _eventTypes;
    int _attempts;

    void Establish()
    {
        _failure = new Exception("Assembly discovery failed.");
        _assembliesProvider.When(_ => _.Initialize()).Do(_ =>
        {
            if (++_attempts == 1) throw _failure;
        });
    }

    void Because()
    {
        _error = Catch.Exception(() => _ = _provider.EventTypes);
        _eventTypes = _provider.EventTypes;
    }

    [Fact] void should_propagate_the_original_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_retry_assembly_initialization() => _attempts.ShouldEqual(2);
    [Fact] void should_not_cache_an_empty_snapshot() => _eventTypes.ShouldContainOnly(typeof(ClientArtifactDiscovered));
    [Fact] void should_complete_all_discovery_steps_on_retry() => _provider.EventTypeMigrators.ShouldContainOnly(typeof(EventTypeMigration<,>));
}
