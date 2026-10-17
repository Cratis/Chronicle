// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

[Collection(PostgreSqlCollection.Name)]
public class with_postgresql_type_set_control(PostgreSqlFixture fixture) : Specification
{
    static readonly EventTypeId _type = new("event-a");
    string _connectionString;
    ObserverStateStorage _storage;
    ObserverTypeSetControl _control;
    KernelObserverState _saved;
    string _columnType;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), () => { })));
        _storage = new(EventStoreName.System, EventStoreNamespaceName.Default, database);
        _control = new(
            new(7),
            new("reactor-subscriber", new(["important"], new("customer"), new("orders"))),
            new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(3), new(42)) },
            [new("connection-one", Guid.Parse("11111111-1111-1111-1111-111111111111"), [new(_type, new(1))], EventGenerationDelivery.Compatibility),
             new("connection-two", null, [new(_type, new(2))], EventGenerationDelivery.Pinned)])
        {
            RetirementResults =
            [
                new(Guid.Parse("11111111-1111-1111-1111-111111111111"), _type, ObserverEventTypeRetirementOutcome.Retired, new(3), []),
                new(Guid.Parse("22222222-2222-2222-2222-222222222222"), new("event-b"), ObserverEventTypeRetirementOutcome.Refused, null, [new("connection-one"), new("connection-two")]),
                new(Guid.Parse("33333333-3333-3333-3333-333333333333"), new("event-c"), ObserverEventTypeRetirementOutcome.NotMember, null, [])
            ],
            PendingChange = new(new(8), new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero), new(42), [new("event-b")], [new(Guid.Parse("11111111-1111-1111-1111-111111111111"), _type)])
        };
    }

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = _control });
        _saved = await _storage.Get("observer");
        await using var context = CreateContext();
        await context.Database.OpenConnectionAsync();
        await using var read = context.Database.GetDbConnection().CreateCommand();
        read.CommandText = "SELECT pg_typeof(\"TypeSetControl\")::text FROM \"Observers\" WHERE \"Id\" = 'observer'";
        _columnType = (string)await read.ExecuteScalarAsync();
    }

    [Fact] void should_use_native_json_storage() => _columnType.ShouldEqual("jsonb");
    [Fact] void should_keep_one_member_for_both_generations() => _saved.TypeSetControl.Members.Count.ShouldEqual(1);
    [Fact] void should_keep_the_membership_revision() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_keep_the_member_incarnation_and_fence() => _saved.TypeSetControl.Members[_type].ShouldEqual(new ObserverTypeSetMember(new(3), new(42)));
    [Fact] void should_keep_the_subscriber_type() => _saved.TypeSetControl.Contract.SubscriberType.ShouldEqual("reactor-subscriber");
    [Fact] void should_keep_the_tag_filter() => _saved.TypeSetControl.Contract.Filters.Tags.ShouldEqual(["important"]);
    [Fact] void should_keep_the_source_type_filter() => _saved.TypeSetControl.Contract.Filters.EventSourceType.Value.ShouldEqual("customer");
    [Fact] void should_keep_the_stream_type_filter() => _saved.TypeSetControl.Contract.Filters.EventStreamType.Value.ShouldEqual("orders");
    [Fact] void should_keep_independent_generation_pins() => _saved.TypeSetControl.Registrations.SelectMany(registration => registration.DeclaredTypes).Select(type => type.Generation.Value).ShouldEqual([1U, 2U]);
    [Fact] void should_keep_independent_delivery_modes() => _saved.TypeSetControl.Registrations.Select(registration => registration.GenerationDelivery).ShouldEqual([EventGenerationDelivery.Compatibility, EventGenerationDelivery.Pinned]);
    [Fact] void should_keep_connection_ids() => _saved.TypeSetControl.Registrations.Select(registration => registration.ConnectionId.Value).ShouldEqual(["connection-one", "connection-two"]);
    [Fact] void should_keep_registration_ids() => _saved.TypeSetControl.Registrations[0].RegistrationId.ShouldEqual(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    [Fact] void should_keep_all_retirement_outcomes() => _saved.TypeSetControl.RetirementResults.Select(result => result.Outcome).ShouldEqual([ObserverEventTypeRetirementOutcome.Retired, ObserverEventTypeRetirementOutcome.Refused, ObserverEventTypeRetirementOutcome.NotMember]);
    [Fact] void should_keep_the_removed_incarnation() => _saved.TypeSetControl.RetirementResults[0].Incarnation.Value.ShouldEqual(3);
    [Fact] void should_keep_the_retired_type() => _saved.TypeSetControl.RetirementResults[0].EventTypeId.ShouldEqual(_type);
    [Fact] void should_keep_the_retirement_operation() => _saved.TypeSetControl.RetirementResults[0].OperationId.ShouldEqual(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    [Fact] void should_keep_declaring_connections() => _saved.TypeSetControl.RetirementResults[1].DeclaringConnections.Select(connection => connection.Value).ShouldEqual(["connection-one", "connection-two"]);
    [Fact] void should_not_invent_a_refused_incarnation() => _saved.TypeSetControl.RetirementResults[1].Incarnation.ShouldBeNull();
    [Fact] void should_not_invent_a_not_member_incarnation() => _saved.TypeSetControl.RetirementResults[2].Incarnation.ShouldBeNull();
    [Fact] void should_keep_the_pending_revision() => _saved.TypeSetControl.PendingChange.Revision.Value.ShouldEqual(8);
    [Fact] void should_keep_the_change_start() => _saved.TypeSetControl.PendingChange.Started.ShouldEqual(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
    [Fact] void should_keep_the_initial_tail() => _saved.TypeSetControl.PendingChange.InitialTail.Value.ShouldEqual(42UL);
    [Fact] void should_keep_pending_additions() => _saved.TypeSetControl.PendingChange.Additions.Select(type => type.Value).ShouldEqual(["event-b"]);
    [Fact] void should_keep_pending_retirements() => _saved.TypeSetControl.PendingChange.Retirements[0].ShouldEqual(new ObserverEventTypeRetirement(Guid.Parse("11111111-1111-1111-1111-111111111111"), _type));

    async Task Destroy() => await fixture.DropDatabase(_connectionString);

    NamespaceDbContext CreateContext() => new(new DbContextOptionsBuilder<NamespaceDbContext>()
        .UseNpgsql(_connectionString).AddConceptAsSupport().Options);
}
