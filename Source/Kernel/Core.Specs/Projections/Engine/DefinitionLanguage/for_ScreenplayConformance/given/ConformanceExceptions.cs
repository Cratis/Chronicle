// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// The intentional differences between Chronicle's lowering and Screenplay's executable semantic model, one per case.
/// </summary>
/// <remarks>
/// A case not listed here must lower equivalently. A listed case must still differ in the listed direction: when an entry
/// stops differing, its spec fails, so the list cannot outlive the differences it records.
/// </remarks>
public static class ConformanceExceptions
{
    /// <summary>
    /// Gets the listed differences by case name.
    /// </summary>
    public static IReadOnlyDictionary<string, ConformanceException> All { get; } = new Dictionary<string, ConformanceException>(StringComparer.Ordinal)
    {
        // Chronicle accepts; the semantic model leaves the construct out on purpose (Screenplay semantic-model.md, "What stays out").
        ["keys-literal-number"] = new(ConformanceDirection.ScreenplayRejects, "A key literal other than text is stored by Chronicle as a property path, so the semantic model admits only 'literal \"...\"' keys (PLAY0268)"),
        ["keys-template"] = new(ConformanceDirection.ScreenplayRejects, "The semantic model has no portable string-template expression, so a template key is not admitted (PLAY0268)"),
        ["mapping-template"] = new(ConformanceDirection.ScreenplayRejects, "The semantic model has no portable string-template expression, so a template mapping is not admitted (PLAY0268)"),
        ["mapping-caused-by"] = new(ConformanceDirection.ScreenplayRejects, "'$causedBy' has no runtime resolver in Chronicle (#4119); the semantic model requires '$eventContext.causedBy.*' (PLAY0268)"),
        ["mapping-dynamic-key"] = new(ConformanceDirection.ScreenplayRejects, "The semantic model has no dictionary-keyed target, so a dynamic dictionary key is not admitted (PLAY0268)"),
        ["sequence"] = new(ConformanceDirection.ScreenplayRejects, "Which event sequence a projection observes is a realization concern, not portable behavior (PLAY0268)"),
        ["parent-outside-children"] = new(ConformanceDirection.ScreenplayRejects, "Chronicle never reads a parent key outside 'children'; the semantic model reports it instead of binding and ignoring it (PLAY0268)"),
        ["context-not-a-member"] = new(ConformanceDirection.ScreenplayRejects, "The semantic model admits only the scalar paths of Chronicle's event context (PLAY0273); Chronicle receives only a Screenplay warning for an unknown member and compiles it"),
        ["context-on-behalf-of"] = new(ConformanceDirection.ScreenplayRejects, "'onBehalfOf' is optional and Chronicle does not read a missing one as null, so the semantic model admits no event-context path through it (PLAY0273)"),

        // The semantic model binds the construct; Chronicle rejects it on purpose.
        ["variant"] = new(ConformanceDirection.ChronicleRejects, "Chronicle's declaration language does not lower variants yet and reports them as unsupported blocks (#4109, full support #3956); the semantic model binds one scoped projection per variant"),
        ["children-all"] = new(ConformanceDirection.ChronicleRejects, "ProjectionValidator admits 'all' only at the projection level; the semantic model binds it below as 'every' with warning PLAY0380, mirroring the visitor dropping the subscription to every event type there"),
        ["removal-root-remove-via-join"] = new(ConformanceDirection.ChronicleRejects, "ProjectionValidator admits 'remove via join' only inside 'children'; the semantic model binds every block at every level and leaves execution to its evaluator"),
        ["removal-nested-remove-via-join"] = new(ConformanceDirection.ChronicleRejects, "ProjectionValidator rejects 'remove via join' inside 'nested'; the semantic model binds every block at every level"),
        ["nested-children-inside"] = new(ConformanceDirection.ChronicleRejects, "ProjectionValidator rejects 'children' inside 'nested'; the semantic model binds every block at every level"),
        ["nested-in-children-join"] = new(ConformanceDirection.ChronicleRejects, "ProjectionValidator rejects 'join' in a nested object under 'children'; the semantic model binds it (and notes that Chronicle does not apply joins inside 'nested', #4125)"),

        // Both reject, so there is no lowering to compare.
        ["every-twice"] = new(ConformanceDirection.BothReject, "One 'every' or 'all' per level: ProjectionValidator and the semantic model (PLAY0268) both reject a second one"),
        ["all-and-every"] = new(ConformanceDirection.BothReject, "One 'every' or 'all' per level: ProjectionValidator and the semantic model (PLAY0268) both reject a second one"),
        ["removal-root-clear-with"] = new(ConformanceDirection.BothReject, "The Screenplay parser accepts 'clear with' only inside 'nested' (PLAY0071), so neither side binds it"),
        ["context-unknown-path"] = new(ConformanceDirection.BothReject, "An event-context path below a collection is a Screenplay parser error (PLAY0297) that both sides report"),

        // The two sides disagree, and it is not known to be intentional.
        ["joins-event-no-automap"] = new(ConformanceDirection.Disagreement, "Screenplay-side fix pending (reported to the Screenplay lane) - the semantic model ignores 'automap'/'no automap' on a joined event (warning PLAY0380 says Chronicle drops it), but the visitor keeps it on JoinDefinition.AutoMap and ProjectionFactory.GetMergedJoinProperties honors it"),
        ["children-every"] = new(ConformanceDirection.Disagreement, "Cratis/Chronicle#4701 - the semantic model keeps include-children on an 'every' below the projection level, while the visitor merges a child level's 'every' into a FromEveryDefinition whose IncludeChildren stays false"),
    };
}
