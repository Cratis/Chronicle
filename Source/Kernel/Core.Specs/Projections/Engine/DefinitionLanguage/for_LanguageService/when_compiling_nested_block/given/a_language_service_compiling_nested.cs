// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_nested_block.given;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

public record SliceCreated(string Name);

public record CommandSetForSlice(string Name, string Schema);

public record CommandUpdatedForSlice(string Schema);

public record CommandClearedForSlice(string Id);

public record ValidationAdded(string Rule);

public record ValidationRemoved(string Id);

public record TaskAdded(string TaskId, string ProjectId, string Title);

public record TaskAssigned(string AssigneeName, string AssigneeEmail);

public record TaskUnassigned(string TaskId);

public record CommandItem(string Name, string Schema);

public record ValidationConfig(string Rule);

public record TaskAssignee(string Name, string Email);

public record SliceTask(string Id, string Title, TaskAssignee? Assignee);

public record SliceReadModel(string Name, CommandItem? Command);

public record SliceWithInnerNestedReadModel(string Name, CommandWithValidation? Command);

public record CommandWithValidation(string Name, string Schema, ValidationConfig? Validation);

public record ProjectWithTasksReadModel(string Name, List<SliceTask>? Tasks);

public abstract class a_language_service_compiling_nested<TReadModel> : Specification
    where TReadModel : class
{
    protected ILanguageService _languageService;
    protected ReadModelDefinition _readModelDefinition;
    protected List<EventTypeSchema> _eventTypeSchemas;

    protected virtual IEnumerable<Type> EventTypes => [];

    void Establish()
    {
        _languageService = new LanguageService(new Generator(), new DeclarativeCodeGenerator(), new ModelBoundCodeGenerator());
        _readModelDefinition = CreateReadModelDefinition<TReadModel>();
        _eventTypeSchemas = CreateEventTypeSchemas(EventTypes).ToList();
    }

    protected ProjectionDefinition Compile(string declaration)
    {
        var result = _languageService.Compile(
            declaration,
            ProjectionOwner.Client,
            [_readModelDefinition],
            _eventTypeSchemas);

        return result.Match(
            projectionDef => projectionDef,
            errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors)}"));
    }

    static ReadModelDefinition CreateReadModelDefinition<T>()
        where T : class
    {
        var schema = JsonSchema.FromType<T>();
        var name = typeof(T).Name;

        return new ReadModelDefinition(
            new ReadModelIdentifier(name),
            new ReadModelContainerName(name),
            new ReadModelDisplayName(name),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new Concepts.Sinks.SinkDefinition(
                new Concepts.Sinks.SinkConfigurationId(Guid.NewGuid()),
                Concepts.Sinks.WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = schema
            },
            []);
    }

    static IEnumerable<EventTypeSchema> CreateEventTypeSchemas(IEnumerable<Type> eventTypes)
    {
        foreach (var eventType in eventTypes)
        {
            var schema = JsonSchema.FromType(eventType);
            yield return new EventTypeSchema(
                (EventType)eventType.Name,
                EventTypeOwner.Client,
                EventTypeSource.Code,
                schema);
        }
    }
}
