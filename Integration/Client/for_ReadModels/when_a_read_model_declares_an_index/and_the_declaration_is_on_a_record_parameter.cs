// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_declares_an_index.and_the_declaration_is_on_a_record_parameter.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_declares_an_index;

/// <summary>
/// The whole point of declaring an index: that one exists in the store afterwards.
/// </summary>
/// <param name="context">The test context.</param>
/// <remarks>
/// Every other spec around indexing stops at the client — that the attribute is collected, that the sink
/// would create what it is given. None of them would notice the chain being broken anywhere between, and it
/// was: <c>[Index]</c> on a positional record parameter bound to the parameter while only properties were
/// inspected, so every declaration collected to an empty list. It compiled, it registered, the sink dutifully
/// created nothing, and a production store ran for months with 78 read models and not one declared index.
/// <para>
/// A declaration that produces no index is indistinguishable from one that works, at every layer except this
/// one. So this spec asks the store.
/// </para>
/// <para>
/// The at-rest assertions are skipped on the SQL backends, where there is no MongoDB container to inspect,
/// and paired with one that fails if the read was skipped on a run where it should have happened — otherwise
/// a spec that silently inspected nothing would pass by asserting against an empty list, which is the same
/// class of quiet success it exists to catch.
/// </para>
/// </remarks>
[Collection(ChronicleCollection.Name)]
public class and_the_declaration_is_on_a_record_parameter(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        const string CollectionName = "IndexedOrders";

        public EventSourceId OrderId { get; } = "indexed-order-1";
        public OrderPlaced Event { get; private set; } = default!;
        public IndexedOrder? Instance { get; private set; }
        public IEnumerable<string> IndexNames { get; private set; } = [];

        public bool StoreCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(OrderPlaced)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(IndexedOrder)];

        void Establish() => Event = new OrderPlaced("customer-42", 199);

        async Task Because()
        {
            await EventStore.EventLog.Append(OrderId, Event);

            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            while (Instance is null)
            {
                Instance = await EventStore.ReadModels.GetInstanceById<IndexedOrder>(OrderId.Value);
                if (Instance is not null) break;
                await Task.Delay(200, cts.Token);
            }

            if (!StoreCanBeInspected)
            {
                return;
            }

            using var indexes = await ChronicleFixture.ReadModels.Database
                .GetCollection<BsonDocument>(CollectionName)
                .Indexes.ListAsync(cts.Token);

            IndexNames = [.. (await indexes.ToListAsync(cts.Token)).Select(index => index["name"].AsString)];
        }
    }

    [Fact] void should_project_the_read_model() => Context.Instance.ShouldNotBeNull();

    [Fact] void should_have_read_the_indexes_when_the_backend_allows_it() =>
        (!Context.StoreCanBeInspected || Context.IndexNames.Any()).ShouldBeTrue();

    [Fact] void should_create_an_index_for_the_declared_property() =>
        (!Context.StoreCanBeInspected || HasIndexFor("CustomerId")).ShouldBeTrue();

    [Fact] void should_not_create_an_index_for_an_undeclared_property() => HasIndexFor("Total").ShouldBeFalse();

    bool HasIndexFor(string property) =>
        Context.IndexNames.Any(name => name.Contains(property, StringComparison.OrdinalIgnoreCase));
}

[EventType]
public record OrderPlaced(string CustomerId, int Total);

[FromEvent<OrderPlaced>]
public record IndexedOrder(string Id, [Index] string CustomerId, int Total);
