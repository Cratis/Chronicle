// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Schemas;
using Cratis.Concepts;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Works out which of the layers the in-process harness substitutes a given read model's shape actually
/// reaches.
/// </summary>
/// <remarks>
/// The harness already knows every one of these before a single event is seeded: they are read off the
/// projection definition and the read model's own type. What it has never done is say so. Reporting only the
/// shape-dependent layers is deliberate — a signal that fires for every scenario is not a signal, and the
/// layers that apply unconditionally belong in documentation, where they can be read once.
/// </remarks>
internal static class SubstitutedLayers
{
    /// <summary>
    /// Detects the substituted layers a read model depends on.
    /// </summary>
    /// <param name="readModelType">The read model CLR type under test.</param>
    /// <param name="projectionDefinition">The projection definition backing it, or <see langword="null"/> when it is reduced rather than projected.</param>
    /// <param name="readModelSchema">The read model's generated <see cref="JsonSchema"/> — the same schema a deployed Chronicle gates compliance on.</param>
    /// <param name="compliancePipelineIsRun">Whether the pipeline that materializes the read model runs Chronicle's compliance stack.</param>
    /// <returns>The substitutions the read model depends on, empty when it depends on none.</returns>
    public static IReadOnlyList<ReadModelSubstitution> DetectFor(
        Type readModelType,
        ProjectionDefinition? projectionDefinition,
        JsonSchema readModelSchema,
        bool compliancePipelineIsRun)
    {
        var substitutions = new List<ReadModelSubstitution>();
        AddDocumentKey(readModelType, substitutions);
        AddCompliance(readModelSchema, compliancePipelineIsRun, substitutions);

        if (projectionDefinition is not null)
        {
            AddRootRemoval(projectionDefinition, substitutions);
            AddJoins(projectionDefinition, substitutions);
            AddParentKeys(projectionDefinition, substitutions);
        }

        return substitutions;
    }

    static void AddDocumentKey(Type readModelType, List<ReadModelSubstitution> substitutions)
    {
        var identifier = IdentifierProperty.Find(readModelType);
        if (identifier is null)
        {
            return;
        }

        var keyType = identifier.PropertyType.IsConcept()
            ? identifier.PropertyType.GetConceptValueType()
            : identifier.PropertyType;

        if (keyType == typeof(Guid))
        {
            return;
        }

        substitutions.Add(new(
            ReadModelSubstitutedLayer.Sink,
            $"the '{identifier.Name}' key of type '{keyType.Name}'",
            "the stored representation of a non-Guid document key is modeled in C# here rather than written and read back, so this tier cannot tell you what the sink does with it"));
    }

    /// <summary>
    /// Reports the compliance layer when the read model carries compliance metadata that nothing in this tier
    /// acts on.
    /// </summary>
    /// <param name="readModelSchema">The read model's generated <see cref="JsonSchema"/>.</param>
    /// <param name="compliancePipelineIsRun">Whether the materializing pipeline runs Chronicle's compliance stack.</param>
    /// <param name="substitutions">The substitutions collected so far.</param>
    /// <remarks>
    /// <c>HasComplianceMetadata()</c> on the read model schema is the exact gate the live projection pipeline
    /// applies before encrypting a changeset and before releasing what it read, so a read model this answers
    /// yes for is one whose values a running system stores as ciphertext. When the pipeline that materializes
    /// it here does the same, there is nothing being stood in for and nothing is reported — the detection
    /// stays so that a pipeline which stops applying compliance surfaces instead of going quiet.
    /// </remarks>
    static void AddCompliance(JsonSchema readModelSchema, bool compliancePipelineIsRun, List<ReadModelSubstitution> substitutions)
    {
        if (compliancePipelineIsRun || !readModelSchema.HasComplianceMetadata())
        {
            return;
        }

        substitutions.Add(new(
            ReadModelSubstitutedLayer.Compliance,
            "compliance metadata on the read model",
            "the values a deployed Chronicle encrypts into the sink and releases on read are projected and read as plaintext here, so neither the encryption, the subject they are keyed by, nor what an erased subject reads back as is exercised"));
    }

    static void AddRootRemoval(ProjectionDefinition definition, List<ReadModelSubstitution> substitutions)
    {
        if (definition.RemovedWith.Count == 0 && definition.RemovedWithJoin.Count == 0)
        {
            return;
        }

        substitutions.Add(new(
            ReadModelSubstitutedLayer.Sink,
            "a root-level [RemovedWith] removal",
            "removing the root document is modeled here by dropping the in-memory state, so the sink's own delete — and what a later event finds afterwards — is not exercised"));
    }

    static void AddJoins(ProjectionDefinition definition, List<ReadModelSubstitution> substitutions)
    {
        if (definition.Join.Count > 0)
        {
            substitutions.Add(new(
                ReadModelSubstitutedLayer.JoinKeyResolution,
                $"a root-level [Join] on '{string.Join("', '", definition.Join.Values.Select(join => join.On))}'",
                "the harness corrects the engine's join key against its own sink instead of resolving it the way a deployed Chronicle does, which is where a join key whose type the store has to parse goes wrong"));
        }

        var joiningChildren = Descendants(definition)
            .Where(child => child.Definition.Join.Count > 0)
            .Select(child => child.Path)
            .ToList();

        if (joiningChildren.Count > 0)
        {
            substitutions.Add(new(
                ReadModelSubstitutedLayer.JoinKeyResolution,
                $"a [Join] on child collection '{string.Join("', '", joiningChildren)}'",
                "the harness re-anchors a child join onto the root it finds in its own sink, so the resolution a deployed Chronicle performs against the store is not the one under test"));
        }
    }

    static void AddParentKeys(ProjectionDefinition definition, List<ReadModelSubstitution> substitutions)
    {
        var crossStreamChildren = Descendants(definition)
            .Where(child => child.Definition.From.Values.Any(from =>
                !string.IsNullOrEmpty(from.ParentKey) &&
                from.ParentKey != WellKnownExpressions.EventSourceId))
            .Select(child => child.Path)
            .ToList();

        if (crossStreamChildren.Count == 0)
        {
            return;
        }

        substitutions.Add(new(
            ReadModelSubstitutedLayer.DeferredKeyHandling,
            $"a cross-stream parent key on child collection '{string.Join("', '", crossStreamChildren)}'",
            "a child whose parent has not arrived yet is retried once here after every other seeded event, where a deployed Chronicle defers the partition and redelivers, so arrival order and redelivery are not modeled"));
    }

    static IEnumerable<(string Path, ChildrenDefinition Definition)> Descendants(ProjectionDefinition definition) =>
        Descendants(definition.Children, string.Empty).Concat(Descendants(definition.Nested, string.Empty));

    static IEnumerable<(string Path, ChildrenDefinition Definition)> Descendants(IDictionary<string, ChildrenDefinition> children, string parentPath) =>
        children.SelectMany(child =>
        {
            var path = parentPath.Length == 0 ? child.Key : $"{parentPath}.{child.Key}";
            return new[] { (path, child.Value) }
                .Concat(Descendants(child.Value.Children, path))
                .Concat(Descendants(child.Value.Nested, path));
        });
}
