// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested_from_pdl.given;

/// <summary>
/// Reusable context that compiles a two-level <c>nested</c> + <c>clear with</c> PDL document
/// into a <see cref="ProjectionDefinition"/> using <see cref="LanguageService"/>.
/// </summary>
/// <remarks>
/// <para>
/// There is currently no in-process integration harness that takes a raw PDL string and runs
/// it through the engine — the engine's integration fixtures register typed
/// <c>IProjectionFor&lt;T&gt;</c> classes, not free-form PDL declarations.
/// </para>
/// <para>
/// Phase 1's <c>when_projecting_with_nested_in_nested</c> specs already prove that a
/// <see cref="ProjectionDefinition"/> with the nested + clear-with structure (built via
/// the declarative <c>.Nested(...)</c> / <c>.ClearWith&lt;T&gt;()</c> API) is processed
/// correctly by the engine — outer / inner are set, updated and cleared as expected.
/// </para>
/// <para>
/// Phase 5 closes the loop by proving that <b>PDL compilation</b> produces a
/// <see cref="ProjectionDefinition"/> with exactly the same nested / clear-with shape.
/// Together, the two phases show that PDL → <see cref="ProjectionDefinition"/> → engine
/// behaves identically to the declarative path that Phase 1 covers.
/// </para>
/// </remarks>
public abstract class a_compiled_pdl_nested_projection : Specifications.Specification
{
    public const string Declaration = """
        projection PdlSlice => PdlDeepNestedSlice
          from PdlDeepNestedSliceCreated
            name = name

          nested command
            from PdlDeepNestedCommandSet
              name = name

            nested validation
              from PdlDeepNestedValidationConfigured
                rules = rules
              from PdlDeepNestedValidationUpdated
                rules = newRules
              clear with PdlDeepNestedValidationRemoved

            clear with PdlDeepNestedCommandCleared
        """;

    protected ILanguageService _languageService;
    protected ReadModelDefinition _readModelDefinition;
    protected List<EventTypeSchema> _eventTypeSchemas;
    protected ProjectionDefinition _projection;
    protected ChildrenDefinition _outerNested;
    protected ChildrenDefinition _innerNested;

    void Establish()
    {
        _languageService = new LanguageService(
            new Generator(),
            new DeclarativeCodeGenerator(),
            new ModelBoundCodeGenerator());

        _readModelDefinition = CreateReadModelDefinition<PdlDeepNestedSlice>();
        _eventTypeSchemas = CreateEventTypeSchemas(
        [
            typeof(PdlDeepNestedSliceCreated),
            typeof(PdlDeepNestedCommandSet),
            typeof(PdlDeepNestedValidationConfigured),
            typeof(PdlDeepNestedValidationUpdated),
            typeof(PdlDeepNestedValidationRemoved),
            typeof(PdlDeepNestedCommandCleared)
        ]).ToList();

        var result = _languageService.Compile(
            Declaration,
            ProjectionOwner.Client,
            [_readModelDefinition],
            _eventTypeSchemas);

        _projection = result.Match(
            definition => definition,
            errors => throw new InvalidOperationException(
                $"PDL compilation failed: {string.Join(", ", errors.Errors.Select(e => e.Message))}"));

        _outerNested = _projection.Nested[(Properties.PropertyPath)"command"];
        _innerNested = _outerNested.Nested[(Properties.PropertyPath)"validation"];
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
            new SinkDefinition(
                new SinkConfigurationId(Guid.NewGuid()),
                WellKnownSinkTypes.MongoDB),
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

/// <summary>
/// Event that creates the outermost slice — sets the slice-level name.
/// </summary>
/// <param name="Name">The name of the slice.</param>
public record PdlDeepNestedSliceCreated(string Name);

/// <summary>
/// Event that sets the outer nested command — name only.
/// </summary>
/// <param name="Name">The name of the command.</param>
public record PdlDeepNestedCommandSet(string Name);

/// <summary>
/// Event that configures the inner nested validation block for the first time.
/// </summary>
/// <param name="Rules">The validation rules expression.</param>
public record PdlDeepNestedValidationConfigured(string Rules);

/// <summary>
/// Event that updates the inner nested validation rules.
/// </summary>
/// <param name="NewRules">The updated validation rules expression.</param>
public record PdlDeepNestedValidationUpdated(string NewRules);

/// <summary>
/// Event that clears the inner nested validation block.
/// </summary>
public record PdlDeepNestedValidationRemoved;

/// <summary>
/// Event that clears the outer nested command block.
/// </summary>
public record PdlDeepNestedCommandCleared;

/// <summary>
/// Inner nested validation read-model shape.
/// </summary>
public class PdlDeepNestedValidationItem
{
    /// <summary>
    /// Gets or sets the validation rules.
    /// </summary>
    public string Rules { get; set; } = string.Empty;
}

/// <summary>
/// Outer nested command read-model shape — owns an optional inner validation block.
/// </summary>
public class PdlDeepNestedCommandItem
{
    /// <summary>
    /// Gets or sets the command name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the inner nested validation block.
    /// </summary>
    public PdlDeepNestedValidationItem? Validation { get; set; }
}

/// <summary>
/// Top-level read model with an optional nested command block.
/// </summary>
/// <param name="Name">The slice name.</param>
/// <param name="Command">The optional nested command.</param>
public record PdlDeepNestedSlice(
    string Name,
    PdlDeepNestedCommandItem? Command);

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
