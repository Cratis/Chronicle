// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.Concepts;
using Cratis.Serialization;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_deferred_child_carries_pii.for_an_added_child.and_the_child_arrives_before_the_root.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_deferred_child_carries_pii.for_an_added_child;

[Collection(ChronicleCollection.Name)]
public class and_the_child_arrives_before_the_root(context context) : Given<context>(context)
{
    const string ChildEmail = "child@example.com";

    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId RootId { get; } = "deferred-pii-added-child-root";
        public ChildId ChildId { get; } = ChildId.New();
        public AddedRoot? Result { get; private set; }
        public BsonDocument? StoredDocument { get; private set; }
        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);
        public override IEnumerable<Type> EventTypes => [typeof(RootCreated), typeof(ChildAddedToRoot)];
        public override IEnumerable<Type> Projections => [typeof(RootProjection)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new RootProjection());

        async Task Because()
        {
            var projection = EventStore.Projections.GetHandlerFor<RootProjection>();
            await projection.WaitTillSubscribed();
            var child = await EventStore.EventLog.Append(RootId, new ChildAddedToRoot(ChildId, ChildEmail, "Child"));
            await projection.WaitTillReachesEventSequenceNumber(child.SequenceNumber);
            var root = await EventStore.EventLog.Append(RootId, new RootCreated("Root"));
            await projection.WaitTillReachesEventSequenceNumber(root.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<AddedRoot>(RootId.Value);

            var namingPolicy = Services.GetService<INamingPolicy>() ?? new DefaultNamingPolicy();
            StoredDocument = await StoredReadModelDocument.Read(ChronicleFixture, namingPolicy.GetReadModelName(typeof(AddedRoot)));
        }
    }

    [Fact] void should_release_the_child_email() => Context.Result!.Children.Single().Email.Value.ShouldEqual(ChildEmail);
    [Fact] void should_keep_the_unprotected_child_name() => Context.Result!.Children.Single().Name.ShouldEqual("Child");
    [Fact] void should_have_read_the_stored_document_when_the_backend_allows_it() => (!Context.DocumentCanBeInspected || Context.StoredDocument is not null).ShouldBeTrue();
    [Fact]
    void should_store_the_child_email_encrypted()
    {
        if (!Context.DocumentCanBeInspected) return;
        var children = StoredReadModelDocument.Field(Context.StoredDocument, "Children")!.AsBsonArray;
        children.Count.ShouldEqual(1);
        var email = StoredReadModelDocument.Field(children[0].AsBsonDocument, "Email");
        email.ShouldNotBeNull();
        email!.AsString.ShouldNotEqual(ChildEmail);
    }
}

public class RootProjection : IProjectionFor<AddedRoot>
{
    public void Define(IProjectionBuilderFor<AddedRoot> builder) => builder
        .From<RootCreated>()
        .Children(_ => _.Children, _ => _
            .IdentifiedBy(child => child.Id)
            .From<ChildAddedToRoot>(from => from.UsingParentKeyFromContext(context => context.EventSourceId).UsingKey(@event => @event.Id)));
}

public class AddedRoot
{
    public string Name { get; set; } = string.Empty;
    public List<Child> Children { get; set; } = [];
}

public class Child
{
    public ChildId Id { get; set; } = ChildId.NotSet;
    public Email Email { get; set; } = new(string.Empty);
    public string Name { get; set; } = string.Empty;
}

[PII]
public record Email(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator Email(string value) => new(value);
}

[EventType]
public record RootCreated(string Name);

[EventType]
public record ChildAddedToRoot(ChildId Id, Email Email, string Name);

#pragma warning restore SA1402
