// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_a_graph.given;

/// <summary>
/// The graph the .NET client spec for_GraphDefinitionBuilder builds from model-bound attributes, written in the
/// projection declaration language. The graph is named Organization, as in Cratis/Screenplay#1159.
/// </summary>
public class an_organization_graph_declaration : Specification
{
    public const string Declaration = """
        concept PersonId : Uuid
        concept PersonName : String

        graph Organization
          node Person
            personId PersonId key
            name PersonName
          node Group
            groupId Uuid key
            name String
            memberCount Int
          node Place
            placeId Uuid key
          edge MEMBER_OF source Person target Group
            since DateTime
          edge VISITED source Person target Place identified by visitId
            visitId Uuid
            times Int

        projection OrganizationGraph => graph Organization
          node Person
            from PersonRegistered key $eventSourceId
            remove with PersonRemoved key $eventSourceId detach
          node Group
            from GroupCreated key $eventSourceId
            from MemberAdded key groupId
              increment memberCount
            from MemberRemoved key groupId
              decrement memberCount
          node Place
            from PlaceVisited key placeId
          edge MEMBER_OF
            from MemberAdded source personId target groupId
              since = $eventContext.occurred
            remove with MemberRemoved source personId target groupId
          edge VISITED
            from PlaceVisited source personId target placeId identified by visitId
              count times
        """;

    protected ILanguageService _languageService;
    protected List<EventTypeSchema> _eventTypeSchemas;

    void Establish()
    {
        _languageService = new LanguageService(new Generator(), CodeGeneration.given.ProjectionCodeGenerators.All());
        _eventTypeSchemas =
        [
            EventTypeSchemaOf<PersonRegistered>(),
            EventTypeSchemaOf<PersonRemoved>(),
            EventTypeSchemaOf<GroupCreated>(),
            EventTypeSchemaOf<MemberAdded>(),
            EventTypeSchemaOf<MemberRemoved>(),
            EventTypeSchemaOf<PlaceVisited>()
        ];
    }

    protected CompiledGraph? CompileGraph(string declaration, out CompilerErrors errors)
    {
        CompilerErrors found = null;
        var compiled = _languageService.CompileGraph(declaration, ProjectionOwner.Client, _eventTypeSchemas).Match<CompiledGraph?>(
            graph => graph,
            failed =>
            {
                found = failed;
                return null;
            });
        errors = found;
        return compiled;
    }

    static EventTypeSchema EventTypeSchemaOf<T>() =>
        new((EventType)typeof(T).Name, EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromType<T>());
}

public record PersonRegistered(Guid PersonId, string Name);

public record PersonRemoved(Guid PersonId);

public record GroupCreated(Guid GroupId, string Name);

public record MemberAdded(Guid PersonId, Guid GroupId);

public record MemberRemoved(Guid PersonId, Guid GroupId);

public record PlaceVisited(Guid PersonId, Guid PlaceId, Guid VisitId);
