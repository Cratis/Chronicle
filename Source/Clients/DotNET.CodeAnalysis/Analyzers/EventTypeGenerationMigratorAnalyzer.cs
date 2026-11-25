// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports an event type at generation 2 or higher that is missing a migrator for one or more of its generation steps.
/// </summary>
/// <remarks>
/// Chronicle requires an <c language="csharp">EventTypeMigration&lt;TUpgrade, TPrevious&gt;</c> for every consecutive
/// generation step from 1 up to the current generation. Without it the kernel rejects the event type at registration.
/// This analyzer surfaces that at build time and says precisely which steps are missing and what to add.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventTypeGenerationMigratorAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.EventTypeGenerationMustHaveMigrator,
        title: "Event type generation is missing a migrator",
        messageFormat: "Event type '{0}' (id '{1}') is at generation {2} but has no migrator for generation step(s): {3}. Chronicle requires an EventTypeMigration<TUpgrade, TPrevious> for every consecutive generation step from 1 up to the current generation, and rejects the event type at registration without them. FIX: {4} Do not just lower the generation number or remove it to silence this: events already stored at earlier generations must be upcast. Only lower it if generation {2} has never been deployed anywhere.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An event type at generation N (N >= 2) must have an EventTypeMigration<TUpgrade, TPrevious> for each step 1->2, 2->3, ... (N-1)->N, where TUpgrade and TPrevious are adjacent generations of the same event type id. Declare each previous generation as a record marked [EventTypeGenerationFor<TCurrent>(generation)] holding the properties that generation had, then add a class deriving EventTypeMigration<TUpgrade, TPrevious> that implements Upcast and Downcast. Without a migrator for every step, Chronicle throws MissingEventTypeMigrators or MissingMigrationForEventTypeGeneration when the event types are registered, so the application fails to start.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var declarations = new ConcurrentBag<Declaration>();
        var migrations = new ConcurrentBag<MigrationKey>();

        context.RegisterSymbolAction(
            symbolContext => Collect((INamedTypeSymbol)symbolContext.Symbol, declarations, migrations),
            SymbolKind.NamedType);

        context.RegisterCompilationEndAction(endContext => Report(endContext, declarations, migrations));
    }

    static void Collect(INamedTypeSymbol type, ConcurrentBag<Declaration> declarations, ConcurrentBag<MigrationKey> migrations)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (TryResolve(type, out var resolved))
        {
            declarations.Add(new Declaration(type, resolved.Id, resolved.Generation, resolved.IsCurrent));
        }

        if (!type.IsAbstract && !type.IsGenericType && TryGetMigrationKey(type, out var key))
        {
            migrations.Add(key);
        }
    }

    static void Report(CompilationAnalysisContext context, ConcurrentBag<Declaration> declarations, ConcurrentBag<MigrationKey> migrations)
    {
        var knownMigrations = new HashSet<MigrationKey>(migrations);
        HashSet<MigrationKey>? referencedMigrations = null;

        foreach (var group in declarations.GroupBy(declaration => declaration.Id))
        {
            var highest = group.Max(declaration => declaration.Generation);
            if (highest < 2)
            {
                continue;
            }

            var current = group
                .Where(declaration => declaration.Generation == highest && declaration.IsCurrent)
                .OrderBy(declaration => declaration.Type.ToDisplayString(), StringComparer.Ordinal)
                .ToArray();

            if (current.Length == 0)
            {
                // The current generation is declared in another assembly, which owns the check.
                continue;
            }

            var missing = FindMissingSteps(group.Key, highest, knownMigrations);
            if (missing.Count == 0)
            {
                continue;
            }

            // The migrators may live in a referenced assembly. Only pay for looking there when something looks missing.
            referencedMigrations ??= CollectReferencedMigrations(context.Compilation);
            missing = FindMissingSteps(group.Key, highest, referencedMigrations, missing);
            if (missing.Count == 0)
            {
                continue;
            }

            foreach (var declaration in current)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    declaration.Type.Locations.FirstOrDefault(),
                    declaration.Type.Name,
                    group.Key,
                    highest.ToString(CultureInfo.InvariantCulture),
                    string.Join(", ", missing.Select(step => $"{step - 1}->{step}")),
                    DescribeFix(declaration.Type, group.Key, highest, missing, group.ToArray())));
            }
        }
    }

    static List<uint> FindMissingSteps(string id, uint highest, HashSet<MigrationKey> migrations, List<uint>? candidates = null)
    {
        var missing = new List<uint>();
        foreach (var step in candidates ?? Enumerable.Range(2, (int)highest - 1).Select(step => (uint)step).ToList())
        {
            if (!migrations.Contains(new MigrationKey(id, step, step - 1)))
            {
                missing.Add(step);
            }
        }

        return missing;
    }

    static string DescribeFix(INamedTypeSymbol current, string id, uint highest, List<uint> missing, Declaration[] declared)
    {
        var parts = new List<string>();
        foreach (var step in missing)
        {
            var previousGeneration = step - 1;
            var upgradeName = step == highest ? current.Name : NameOfGeneration(declared, step) ?? GenerationName(current, step);
            var previousName = NameOfGeneration(declared, previousGeneration) ?? GenerationName(current, previousGeneration);

            var instructions = new List<string>();
            if (step != highest && NameOfGeneration(declared, step) is null)
            {
                instructions.Add($"declare record '{upgradeName}' with the properties generation {step} had, marked [EventTypeGenerationFor<{current.Name}>({step})]");
            }

            if (NameOfGeneration(declared, previousGeneration) is null)
            {
                instructions.Add($"declare record '{previousName}' with the properties generation {previousGeneration} had, marked [EventTypeGenerationFor<{current.Name}>({previousGeneration})]");
            }

            instructions.Add($"add 'public class {previousName}To{upgradeName}Migration : EventTypeMigration<{upgradeName}, {previousName}>' (namespace Cratis.Chronicle.Events.Migrations) overriding Upcast(IEventMigrationBuilder<{upgradeName}, {previousName}> builder) and Downcast(IEventMigrationBuilder<{previousName}, {upgradeName}> builder) to map {previousName} to {upgradeName} and back");
            parts.Add($"For step {previousGeneration}->{step}: {string.Join(", then ", instructions)}.");
        }

        return $"for event type id '{id}', {string.Join(" ", parts)}";
    }

    static string? NameOfGeneration(Declaration[] declared, uint generation) =>
        declared.FirstOrDefault(declaration => declaration.Generation == generation)?.Type.Name;

    static string GenerationName(INamedTypeSymbol current, uint generation) => $"{current.Name}V{generation}";

    static HashSet<MigrationKey> CollectReferencedMigrations(Compilation compilation)
    {
        var keys = new HashSet<MigrationKey>();
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly ||
                !assembly.Modules.Any(module => module.ReferencedAssemblies.Any(referenced => referenced.Name == "Cratis.Chronicle")))
            {
                continue;
            }

            Walk(assembly.GlobalNamespace, keys);
        }

        return keys;
    }

    static void Walk(INamespaceSymbol namespaceSymbol, HashSet<MigrationKey> keys)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            WalkType(type, keys);
        }

        foreach (var child in namespaceSymbol.GetNamespaceMembers())
        {
            Walk(child, keys);
        }
    }

    static void WalkType(INamedTypeSymbol type, HashSet<MigrationKey> keys)
    {
        if (type.TypeKind == TypeKind.Class && !type.IsAbstract && !type.IsGenericType && TryGetMigrationKey(type, out var key))
        {
            keys.Add(key);
        }

        foreach (var nested in type.GetTypeMembers())
        {
            WalkType(nested, keys);
        }
    }

    static bool TryGetMigrationKey(INamedTypeSymbol type, out MigrationKey key)
    {
        key = default;
        if (!TryGetMigrationEndpoints(type, out var upgrade, out var previous) ||
            !TryResolve(upgrade, out var upgradeResolved) ||
            !TryResolve(previous, out var previousResolved) ||
            upgradeResolved.Id != previousResolved.Id)
        {
            return false;
        }

        key = new MigrationKey(upgradeResolved.Id, upgradeResolved.Generation, previousResolved.Generation);
        return true;
    }

    static bool TryGetMigrationEndpoints(INamedTypeSymbol type, out ITypeSymbol upgrade, out ITypeSymbol previous)
    {
        upgrade = null!;
        previous = null!;

        var migrationBase = WellKnownTypes.GetEventTypeMigrationBase(type);
        if (migrationBase?.TypeArguments.Length == 2)
        {
            upgrade = migrationBase.TypeArguments[0];
            previous = migrationBase.TypeArguments[1];
            return true;
        }

        var migrationInterface = type.AllInterfaces.FirstOrDefault(
            candidate => candidate.IsGenericType &&
                         candidate.TypeArguments.Length == 2 &&
                         candidate.OriginalDefinition.ToDisplayString() == WellKnownTypes.EventTypeMigrationForGenericDisplay);
        if (migrationInterface is null)
        {
            return false;
        }

        upgrade = migrationInterface.TypeArguments[0];
        previous = migrationInterface.TypeArguments[1];
        return true;
    }

    static bool TryResolve(ITypeSymbol type, out ResolvedGeneration resolved)
    {
        resolved = default;

        var generationFor = WellKnownTypes.GetEventTypeGenerationForAttributeData(type);
        if (generationFor is not null)
        {
            var target = WellKnownTypes.GetEventTypeGenerationForTarget(generationFor);
            var targetAttribute = target is null ? null : WellKnownTypes.GetEventTypeAttributeData(target);
            if (target is null || targetAttribute is null || generationFor.ConstructorArguments.Length == 0)
            {
                return false;
            }

            var generation = ToGeneration(generationFor.ConstructorArguments[0].Value);
            resolved = new ResolvedGeneration(WellKnownTypes.GetEventTypeExplicitId(targetAttribute) ?? target.Name, generation, false);
            return true;
        }

        var attribute = WellKnownTypes.GetEventTypeAttributeData(type);
        if (attribute is null)
        {
            return false;
        }

        // Omitted optional arguments are either absent from ConstructorArguments or filled with their defaults; handle both.
        var eventTypeGeneration = attribute.ConstructorArguments.Length > 1
            ? ToGeneration(attribute.ConstructorArguments[1].Value)
            : 1u;

        resolved = new ResolvedGeneration(WellKnownTypes.GetEventTypeExplicitId(attribute) ?? type.Name, eventTypeGeneration, true);
        return true;
    }

    static uint ToGeneration(object? value) => value is IConvertible convertible ? convertible.ToUInt32(CultureInfo.InvariantCulture) : 1u;

    readonly struct ResolvedGeneration(string id, uint generation, bool isCurrent)
    {
        public string Id { get; } = id;

        public uint Generation { get; } = generation;

        public bool IsCurrent { get; } = isCurrent;
    }

    readonly struct MigrationKey(string id, uint upgrade, uint previous) : IEquatable<MigrationKey>
    {
        public string Id { get; } = id;

        public uint Upgrade { get; } = upgrade;

        public uint Previous { get; } = previous;

        public bool Equals(MigrationKey other) => Id == other.Id && Upgrade == other.Upgrade && Previous == other.Previous;

        public override bool Equals(object? obj) => obj is MigrationKey other && Equals(other);

        public override int GetHashCode() => (Id, Upgrade, Previous).GetHashCode();
    }

    sealed class Declaration(INamedTypeSymbol type, string id, uint generation, bool isCurrent)
    {
        public INamedTypeSymbol Type { get; } = type;

        public string Id { get; } = id;

        public uint Generation { get; } = generation;

        public bool IsCurrent { get; } = isCurrent;
    }
}
