// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Screenplay;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_pinned_screenplay_examples : for_LanguageService.given.a_language_service
{
    static readonly string _corpus = Path.Combine(AppContext.BaseDirectory, "Projections", "Engine", "DefinitionLanguage", "ScreenplayExamples");
    static readonly IReadOnlyDictionary<string, string> _rejected = new Dictionary<string, string>
    {
        ["Documentation/screenplay/projections/counters.mdx:173"] = "The combined counter example subscribes to TaskAdded twice at the same level; Chronicle rejects duplicate event handlers",
        ["Documentation/screenplay/projections/removal.md:64"] = "A root-level remove via join violates Chronicle's ProjectionValidator level restriction; join removal is only supported in children",
        ["Documentation/screenplay/projections/removal.md:80"] = "A root-level remove via join violates Chronicle's ProjectionValidator level restriction; join removal is only supported in children",
        ["Documentation/screenplay/projections/removal.md:146"] = "A root-level remove via join violates Chronicle's ProjectionValidator level restriction; join removal is only supported in children",
        ["Documentation/screenplay/projections/removal.md:192"] = "A root-level remove via join violates Chronicle's ProjectionValidator level restriction; join removal is only supported in children",
        ["Documentation/screenplay/projections/removal.md:216"] = "A root-level remove via join violates Chronicle's ProjectionValidator level restriction; join removal is only supported in children",
        ["Documentation/screenplay/projections/removal.md:240"] = "A root-level remove via join violates Chronicle's ProjectionValidator level restriction; join removal is only supported in children",
        ["Documentation/screenplay/projections/removal.md:262"] = "A root-level remove via join violates Chronicle's ProjectionValidator level restriction; join removal is only supported in children",
        ["Documentation/screenplay/projections/removal.md:310"] = "A root-level remove via join violates Chronicle's ProjectionValidator level restriction; join removal is only supported in children",
        ["Documentation/screenplay/projections/removal.md:356"] = "A root-level remove via join violates Chronicle's ProjectionValidator level restriction; join removal is only supported in children",
        ["Documentation/screenplay/projections/variants.md:43"] = "Chronicle reports unsupported variant blocks as compiler errors (#4109); declaration-language variant lowering is not implemented",
        ["Documentation/screenplay/projections/variants.md:58"] = "Chronicle reports unsupported variant blocks as compiler errors (#4109); declaration-language variant lowering is not implemented",
        ["Documentation/screenplay/projections/variants.md:76"] = "Chronicle reports unsupported variant blocks as compiler errors (#4109); declaration-language variant lowering is not implemented"
    };

    JsonDocument _provenance;
    List<string> _failures;

    void Establish() => _provenance = JsonDocument.Parse(File.ReadAllText(Path.Combine(_corpus, "provenance.json")));

    void Because()
    {
        _failures = [];
        var examples = _provenance.RootElement.GetProperty("examples").EnumerateArray().ToArray();
        examples.ShouldNotBeEmpty();
        foreach (var example in examples)
        {
            var reference = $"{example.GetProperty("path").GetString()}:{example.GetProperty("line").GetInt32()}";
            var source = File.ReadAllText(Path.Combine(_corpus, example.GetProperty("file").GetString()!));
            var result = _languageService.Compile(source, ProjectionOwner.Client, [], []);
            var errors = result.Match(_ => CompilerErrors.Empty, _ => _);
            var rejected = _rejected.TryGetValue(reference, out var reason);
            if (errors.HasErrors != rejected || (rejected && string.IsNullOrWhiteSpace(reason)))
            {
                _failures.Add($"{reference}: {string.Join("; ", errors.Errors.Select(_ => _.Message))} (allow-list: {reason ?? "none"})");
            }
        }

        var references = examples.Select(example => $"{example.GetProperty("path").GetString()}:{example.GetProperty("line").GetInt32()}").ToHashSet();
        _failures.AddRange(_rejected.Keys.Where(reference => !references.Contains(reference)).Select(reference => $"Stale allow-list entry: {reference}"));
    }

    [Fact]
    void should_match_the_loaded_screenplay_version_and_commit()
    {
        var version = typeof(ScreenplayCompiler).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        version.ShouldEqual($"{_provenance.RootElement.GetProperty("version").GetString()}+{_provenance.RootElement.GetProperty("commit").GetString()}");
    }

    [Fact] void should_compile_every_example_without_unexpected_errors_or_escaping_exceptions() => Assert.True(_failures.Count == 0, string.Join(Environment.NewLine, _failures));

    void Destroy() => _provenance.Dispose();
}
