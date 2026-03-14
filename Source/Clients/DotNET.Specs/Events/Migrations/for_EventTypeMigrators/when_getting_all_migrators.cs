// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigrators;

public class when_getting_all_migrators : given.all_dependencies
{
    EventTypeMigrators _migrators;
    IEnumerable<Type> _result;

    void Establish()
    {
        _clientArtifactsProvider.EventTypeMigrators.Returns([typeof(TestMigrator)]);
        _migrators = new EventTypeMigrators(_clientArtifactsProvider, _serviceProvider);
    }

    void Because() => _result = _migrators.AllMigrators;

    [Fact] void should_return_migrators_from_client_artifacts_provider() => _result.ShouldContain(typeof(TestMigrator));

    class TestEvent;

    class TestMigrator : IEventTypeMigrationFor<TestEvent>
    {
        public EventTypeGeneration From => 1;
        public EventTypeGeneration To => 2;
        public void Upcast(IEventMigrationBuilder builder) { }
        public void Downcast(IEventMigrationBuilder builder) { }
    }
}
