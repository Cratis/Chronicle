// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// One corpus case: a <c language="csharp">.play</c> application whose single slice declares the events, read model and the projection under
/// comparison. The projection's own text is cut out of the application and given to Chronicle unchanged, so both lowerings
/// read the same declaration.
/// </summary>
/// <param name="Name">The case name, which is the file name without extension.</param>
/// <param name="Application">The full <c language="csharp">.play</c> application text.</param>
/// <param name="Projection">The projection declaration fed to Chronicle.</param>
public record ConformanceCase(string Name, string Application, string Projection)
{
    /// <summary>
    /// The folder holding the corpus, relative to the spec assembly.
    /// </summary>
    public static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "Projections", "Engine", "DefinitionLanguage", "for_ScreenplayConformance", "Cases");

    /// <summary>
    /// Gets the names of every case in the corpus.
    /// </summary>
    public static IEnumerable<string> Names => Directory.GetFiles(Folder, "*.play").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal)!;

    /// <summary>
    /// Loads a case.
    /// </summary>
    /// <param name="name">The case name.</param>
    /// <returns>The case.</returns>
    public static ConformanceCase Load(string name)
    {
        var application = File.ReadAllText(Path.Combine(Folder, $"{name}.play")).ReplaceLineEndings("\n");
        return new(name, application, ExtractProjection(application));
    }

    /// <summary>
    /// Compiles the case both ways and compares the results.
    /// </summary>
    /// <returns>The comparison.</returns>
    public ConformanceResult Compare()
    {
        var chronicle = new LanguageService(new Generator(), CodeGeneration.given.ProjectionCodeGenerators.All())
            .Compile(Projection, ProjectionOwner.Client, [], []);
        var definition = chronicle.Match(_ => _, _ => null!);
        var chronicleErrors = chronicle.Match(_ => [], errors => errors.Errors.Select(_ => $"{_.Line}:{_.Column} {_.Message}").ToArray());

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Conformance"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("case"), "case", $"{Name}.play", Application);
        var screenplay = new SemanticModelCompiler().Compile("Conformance", SemanticDocumentSet.Create([document], catalog));
        var screenplayErrors = screenplay.Diagnostics
            .Where(_ => _.Severity == DiagnosticSeverity.Error)
            .Select(_ => $"{_.Code} {_.Location.Line}:{_.Location.Column} {_.Message}")
            .ToArray();
        if (screenplay.Value is null && screenplayErrors.Length == 0)
        {
            screenplayErrors = ["The semantic model compiler returned no model"];
        }

        if (chronicleErrors.Length > 0 || screenplayErrors.Length > 0)
        {
            return new(Outcome(chronicleErrors.Length > 0, screenplayErrors.Length > 0), chronicleErrors, screenplayErrors, string.Empty, string.Empty);
        }

        var application = screenplay.Value!.Model.Application;
        var slice = Slices(application.Modules.SelectMany(_ => _.Features)).Single(_ => !_.Projections.IsEmpty);
        var projection = slice.Projections.Single();
        var chronicleTree = ChronicleNormalizer.Normalize(definition, ConformanceSchemas.For(application, slice, projection)).Render();
        var screenplayTree = new ScreenplayNormalizer(application, slice).Normalize(projection).Render();

        return new(
            chronicleTree == screenplayTree ? ConformanceOutcome.Equivalent : ConformanceOutcome.Differs,
            [],
            [],
            chronicleTree,
            screenplayTree);
    }

    static ConformanceOutcome Outcome(bool chronicleRejects, bool screenplayRejects) => (chronicleRejects, screenplayRejects) switch
    {
        (true, true) => ConformanceOutcome.BothReject,
        (true, false) => ConformanceOutcome.ChronicleRejects,
        _ => ConformanceOutcome.ScreenplayRejects
    };

    static IEnumerable<SemanticSlice> Slices(IEnumerable<SemanticFeature> features) =>
        features.SelectMany(_ => _.Slices.Concat(Slices(_.Features)));

    static string ExtractProjection(string application)
    {
        var lines = application.Split('\n');
        var start = Array.FindIndex(lines, _ => _.TrimStart().StartsWith("projection ", StringComparison.Ordinal));
        var indent = lines[start].Length - lines[start].TrimStart().Length;
        var body = lines
            .Skip(start + 1)
            .TakeWhile(_ => _.Trim().Length == 0 || _.Length - _.TrimStart().Length > indent);
        return string.Join('\n', new[] { lines[start] }.Concat(body).Select(_ => _.Length >= indent ? _[indent..] : _.TrimStart())).TrimEnd() + "\n";
    }
}
