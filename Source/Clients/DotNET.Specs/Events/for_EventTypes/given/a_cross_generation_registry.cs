// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Registrations;
using Cratis.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

using ProjectionCollection = Cratis.Chronicle.Projections.Projections;

namespace Cratis.Chronicle.Events.for_EventTypes.given;

public class a_cross_generation_registry : all_dependencies
{
    protected const string Consumer = "consumer";
    protected const string PreviousOwner = "previous-owner";
    protected const string CurrentOwner = "current-owner";
    protected const string EventId = "cross-generation-event";
    protected const string JoinId = "cross-generation-join";
    protected const string OverrideOwner = "override-owner";
    protected const string OverrideInbox = "inbox-override-owner";
    protected const string CustomSequence = "cross-generation-custom";
    protected EventTypes _combinedRegistry;

    void Establish() => _eventStore.Name.Returns((EventStoreName)Consumer);

    protected Task<EventTypes> CreateAttributeRegistry() => PrepareRegistry([typeof(AttributePrevious), typeof(AttributeCurrent)]);

    protected Task<EventTypes> CreateExplicitRegistry() => PrepareRegistry([typeof(ExplicitPrevious), typeof(ExplicitCurrent)], ExplicitPair());

    protected Task<EventTypes> CreateOwnerlessRegistry() => PrepareRegistry([typeof(OwnerlessPrevious), typeof(AttributeCurrent)]);

    protected Task<EventTypes> CreateAliasRegistry(bool historicalOwnerIsExplicit)
    {
        var options = new ExplicitArtifacts();
        options.RegisterEventType<ExplicitCurrent>(eventType => eventType.WithId(EventId).Generation(2).ForEventStore(CurrentOwner));
        if (historicalOwnerIsExplicit)
        {
            options.RegisterEventType<OwnedAlias>(eventType => eventType.ForEventStore(PreviousOwner));
            return PrepareRegistry([typeof(OwnedAlias), typeof(ExplicitCurrent)], options);
        }

        return PrepareRegistry([typeof(InheritedAlias), typeof(ExplicitCurrent)], options);
    }

    protected static ProjectionBuilderFor<Model> NewFluentBuilder(EventTypes registry) =>
        new("cross-generation-projection", typeof(Model), new DefaultNamingPolicy(), registry, new JsonSerializerOptions());

    protected async Task<ProjectionCollection> CreateCombinedProjections()
    {
        var discovered = Substitute.For<IClientArtifactsProvider>();
        discovered.Projections.Returns([
            typeof(PreviousProjection), typeof(CurrentProjection), typeof(DuplicateCurrentProjection),
            typeof(InboxProjection), typeof(CustomProjection)
        ]);
        discovered.ModelBoundProjections.Returns([
            typeof(ExplicitPreviousModel), typeof(ExplicitCurrentModel), typeof(InboxModel), typeof(CustomModel)
        ]);
        var options = ExplicitPair();
        options.RegisterEventType<JoinEvent>(eventType => eventType.WithId(JoinId).ForEventStore(CurrentOwner));
        _combinedRegistry = await PrepareRegistry([typeof(ExplicitPrevious), typeof(ExplicitCurrent), typeof(JoinEvent)], options, discovered);
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.ActivateNonDisposable<IProjectionFor<PreviousFluentModel>>(typeof(PreviousProjection)).Returns(new PreviousProjection());
        activator.ActivateNonDisposable<IProjectionFor<CurrentFluentModel>>(typeof(CurrentProjection)).Returns(new CurrentProjection());
        activator.ActivateNonDisposable<IProjectionFor<DuplicateFluentModel>>(typeof(DuplicateCurrentProjection)).Returns(new DuplicateCurrentProjection());
        activator.ActivateNonDisposable<IProjectionFor<InboxFluentModel>>(typeof(InboxProjection)).Returns(new InboxProjection());
        activator.ActivateNonDisposable<IProjectionFor<CustomFluentModel>>(typeof(CustomProjection)).Returns(new CustomProjection());

        return new ProjectionCollection(_eventStore, _combinedRegistry, discovered, new DefaultNamingPolicy(), activator, new JsonSerializerOptions(), NullLogger<ProjectionCollection>.Instance);
    }

    async Task<EventTypes> PrepareRegistry(Type[] types, ExplicitArtifacts? options = null, IClientArtifactsProvider? discovered = null)
    {
        discovered ??= Substitute.For<IClientArtifactsProvider>();
        discovered.EventTypes.Returns(types);
        foreach (var type in types)
        {
            var property = type.GetProperties().Single().Name;
            var schema = await JsonSchema.FromJsonAsync($$"""{"title":"{{type.Name}}","type":"object","properties":{"{{property}}":{"type":"string"}}}""");
            _schemaGenerator.Generate(type).Returns(schema);
        }
        var catalog = options is null ? discovered : options.CreateSnapshot().ApplyTo(discovered);
        var registry = new EventTypes(_eventStore, _schemaGenerator, catalog, _eventTypeMigrators);
        await registry.Discover();

        return registry;
    }

    static ExplicitArtifacts ExplicitPair()
    {
        var options = new ExplicitArtifacts();
        options.RegisterEventType<ExplicitPrevious>(eventType => eventType.WithId(EventId).Generation(1).ForEventStore(PreviousOwner));
        options.RegisterEventType<ExplicitCurrent>(eventType => eventType.WithId(EventId).Generation(2).ForEventStore(CurrentOwner));

        return options;
    }

    [EventType(EventId, generation: 1)]
    [EventStore(PreviousOwner)]
    public record AttributePrevious(string PreviousValue);

    [EventType(EventId, generation: 2)]
    [EventStore(CurrentOwner)]
    public record AttributeCurrent(string CurrentValue);

    public record ExplicitPrevious(string PreviousValue);
    public record ExplicitCurrent(string CurrentValue);
    public record JoinEvent(string JoinedValue);

    [EventType(EventId, generation: 1)]
    public record OwnerlessPrevious(string PreviousValue);

    [EventTypeGenerationFor<ExplicitCurrent>(1)]
    public record InheritedAlias(string PreviousValue);

    [EventTypeGenerationFor<ExplicitCurrent>(1)]
    public record OwnedAlias(string PreviousValue);

    public record Model(string PreviousValue, string CurrentValue);

    [FromEvent<AttributePrevious>]
    public record AttributePreviousModel([Key] Guid Id, string PreviousValue);

    [FromEvent<AttributeCurrent>]
    public record AttributeCurrentModel([Key] Guid Id, string CurrentValue);

    [FromEvent<ExplicitPrevious>]
    public record ExplicitPreviousModel([Key] Guid Id, string PreviousValue);

    [FromEvent<ExplicitCurrent>]
    public record ExplicitCurrentModel([Key] Guid Id, string CurrentValue);

    [FromEvent<ExplicitPrevious>]
    [EventSequence(OverrideInbox)]
    public record InboxModel([Key] Guid Id, string PreviousValue);

    [FromEvent<ExplicitCurrent>]
    [EventSequence(CustomSequence)]
    public record CustomModel([Key] Guid Id, string CurrentValue);

    public record PreviousFluentModel(string PreviousValue);
    public record CurrentFluentModel(string CurrentValue, string JoinedValue);
    public record DuplicateFluentModel(string CurrentValue);
    public record InboxFluentModel(string PreviousValue);
    public record CustomFluentModel(string CurrentValue);

    public class PreviousProjection : IProjectionFor<PreviousFluentModel>
    {
        public void Define(IProjectionBuilderFor<PreviousFluentModel> builder) => builder.From<ExplicitPrevious>();
    }

    public class CurrentProjection : IProjectionFor<CurrentFluentModel>
    {
        public void Define(IProjectionBuilderFor<CurrentFluentModel> builder)
        {
            builder.From<ExplicitCurrent>();
            builder.Join<ExplicitCurrent>();
            builder.Join<JoinEvent>();
        }
    }

    public class DuplicateCurrentProjection : IProjectionFor<DuplicateFluentModel>
    {
        public void Define(IProjectionBuilderFor<DuplicateFluentModel> builder) => builder.From<ExplicitCurrent>();
    }

    public class InboxProjection : IProjectionFor<InboxFluentModel>
    {
        public void Define(IProjectionBuilderFor<InboxFluentModel> builder)
        {
            builder.From<ExplicitPrevious>();
            builder.FromEventSequence(OverrideInbox);
        }
    }

    public class CustomProjection : IProjectionFor<CustomFluentModel>
    {
        public void Define(IProjectionBuilderFor<CustomFluentModel> builder)
        {
            builder.From<ExplicitCurrent>();
            builder.FromEventSequence(CustomSequence);
        }
    }
}
