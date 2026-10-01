// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.for_ReadModels;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_a_root_derivative_is_also_handled_by_a_child.and_the_event_is_registered_for_the_root_as_a_derivative.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_a_root_derivative_is_also_handled_by_a_child;

/// <summary>
/// An event the root handles through a base event type, and a child collection also handles, still creates the root
/// read model's instance: the stored root is initialized and carries the projection's initial values.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_event_is_registered_for_the_root_as_a_derivative(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<DepotWidgetProjection, DepotWidget>(chronicleFixture)
    {
        public BsonDocument? StoredDocument { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(WidgetRegistered), typeof(WidgetRegisteredAtDepot)];

        void Establish() => EventsToAppend.Add(new WidgetRegisteredAtDepot("Sprocket", "part-1"));

        async Task Because() => StoredDocument = await StoredReadModelDocument.ReadWhen(
            ChronicleFixture,
            nameof(DepotWidget),
            document => StoredReadModelDocument.Field(document, "parts") is BsonArray { Count: 1 });
    }

    [Fact] void should_project_the_root_property() => Context.Result.Name.ShouldEqual("Sprocket");
    [Fact] void should_project_the_child() => Context.Result.Parts.Single().Label.ShouldEqual("Sprocket");
    [Fact] void should_apply_the_initial_values() => Context.Result.Status.ShouldEqual("pending");

    [Fact] void should_have_read_the_stored_document_when_the_backend_allows_it() =>
        (!Context.DocumentCanBeInspected || Context.StoredDocument is not null).ShouldBeTrue();

    [Fact] void should_store_the_root_as_initialized() =>
        (!Context.DocumentCanBeInspected || StoredReadModelDocument.Field(Context.StoredDocument, "__initialized") is BsonBoolean { Value: true }).ShouldBeTrue();

    [Fact] void should_store_the_initial_values() =>
        (!Context.DocumentCanBeInspected || string.Equals(StoredReadModelDocument.Field(Context.StoredDocument, "status")?.ToString(), "pending", StringComparison.Ordinal)).ShouldBeTrue();
}

public class DepotWidgetProjection : IProjectionFor<DepotWidget>
{
    public void Define(IProjectionBuilderFor<DepotWidget> builder) => builder
        .WithInitialValues(() => new DepotWidget(string.Empty, string.Empty, "pending", []))
        .From<WidgetRegistered>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(m => m.Parts, c => c
            .IdentifiedBy(m => m.PartId)
            .From<WidgetRegisteredAtDepot>(b => b
                .UsingKey(e => e.PartId)
                .Set(m => m.Label).To(e => e.Name)));
}

[EventType]
public record WidgetRegistered(string Name);

[EventType]
public record WidgetRegisteredAtDepot(string Name, string PartId) : WidgetRegistered(Name);

public record DepotPart(string PartId, string Label);

public record DepotWidget(string Id, string Name, string Status, IEnumerable<DepotPart> Parts);

#pragma warning restore SA1402
