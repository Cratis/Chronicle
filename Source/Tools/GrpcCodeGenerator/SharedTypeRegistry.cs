// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Tracks Core-owned types that need a generated mirror in the contracts, so a value shared across several
/// artifacts - an enum, a plain record used by more than one command or read model - has somewhere to come from
/// other than being hand-written in Contracts and referenced back into Core.
/// </summary>
/// <remarks>
/// A <see cref="Type"/> reaching <see cref="TypeHelper.GetTypeName"/> that is not a primitive, a collection, a
/// concept, or a read model is normally assumed to already exist as a contract - true for everything Contracts
/// itself declares, false for a type Core declares. This tells the two apart and remembers every one it finds, so
/// a later pass in <c>Program</c> can generate each one exactly once, transitively, after every service has had a
/// chance to reference it. The generator is a single-shot CLI process (one run, then it exits), so static state
/// here carries no concurrency risk - the same simplification <see cref="TransportTypes"/> already relies on.
/// <para>
/// Candidacy is decided by namespace, not by which assembly loaded the type. A Core artifact can reference a type
/// from a project Core itself depends on - <c>Concepts.Jobs.JobStatus</c> is a real example, already the type
/// Core's own storage layer uses - and that is exactly as much "not a contract yet" as a type Core declares
/// directly. Gating on the Core assembly alone would miss it and try to invent a second, redundant mirror.
/// </para>
/// </remarks>
public static class SharedTypeRegistry
{
    /// <summary>
    /// Kernel projects Core depends on and reuses types from, that sit as an extra namespace segment between the
    /// Chronicle root and the area name - "Cratis.Chronicle.Concepts.Jobs" mirrors into "Cratis.Chronicle.Contracts.Jobs",
    /// not "...Contracts.Concepts.Jobs". Core's own types have no such segment ("Cratis.Chronicle.Jobs" already
    /// mirrors directly), so this only fires for a type reused from one of these known internal layers.
    /// </summary>
    static readonly HashSet<string> _transparentLayerSegments = new(StringComparer.Ordinal) { "Concepts" };

    static readonly Dictionary<Type, string> _discovered = new();
    static int _skipNamespaceSegments;
    static string _baseNamespace = string.Empty;
    static string _chronicleRootNamespace = string.Empty;

    /// <summary>
    /// Gets every Core-owned type discovered so far, mapped to its fully qualified contract name.
    /// </summary>
    public static IReadOnlyDictionary<Type, string> Discovered => _discovered;

    /// <summary>
    /// Configures the registry for one generation run. Call once, before any type name is resolved.
    /// </summary>
    /// <param name="skipNamespaceSegments">The number of leading namespace segments to skip, same as the services use.</param>
    /// <param name="baseNamespace">The base namespace to prepend to generated types, same as the services use.</param>
    public static void Configure(int skipNamespaceSegments, string baseNamespace)
    {
        _skipNamespaceSegments = skipNamespaceSegments;
        _baseNamespace = baseNamespace;

        // The base namespace names where contracts land ("Cratis.Chronicle.Contracts"); everything Chronicle owns
        // shares its parent ("Cratis.Chronicle"). Deriving the root this way needs no separate CLI argument and
        // stays correct if the base namespace ever changes.
        var lastSegment = baseNamespace.LastIndexOf('.');
        _chronicleRootNamespace = lastSegment > 0 ? baseNamespace[..lastSegment] : baseNamespace;

        _discovered.Clear();
    }

    /// <summary>
    /// Gets the fully qualified contract name for a Core-owned type, registering it for generation the first time
    /// it is seen.
    /// </summary>
    /// <param name="type">The type a member declares.</param>
    /// <returns>The <c>global::</c>-qualified contract name, or null when the type is not a shared-type candidate.</returns>
    public static string? QualifiedNameFor(Type type)
    {
        if (!IsCandidate(type))
        {
            return null;
        }

        if (!_discovered.TryGetValue(type, out var qualifiedName))
        {
            qualifiedName = $"global::{MapNamespace(type.Namespace ?? string.Empty)}.{type.Name}";
            _discovered[type] = qualifiedName;
        }

        return qualifiedName;
    }

    /// <summary>
    /// Maps a Core namespace onto the contract namespace it mirrors into, using the same skip/base transform the
    /// per-service generation already applies - so a type Core places under (for example)
    /// <c>Cratis.Chronicle.Jobs</c> lands under <c>Cratis.Chronicle.Contracts.Jobs</c>, exactly where it already
    /// lives today.
    /// </summary>
    /// <param name="sourceNamespace">The type's own namespace.</param>
    /// <returns>The target contract namespace.</returns>
    public static string MapNamespace(string sourceNamespace)
    {
        var segments = sourceNamespace.Split('.');
        var skipped = segments.Skip(_skipNamespaceSegments).ToArray();

        if (skipped.Length > 0 && _transparentLayerSegments.Contains(skipped[0]))
        {
            skipped = skipped.Skip(1).ToArray();
        }

        // A type declared with nothing left after skipping (at the Chronicle root itself, or directly under a
        // transparent layer with no area segment beneath it) mirrors straight into the base namespace - joining
        // an empty segment list would otherwise leave a dangling trailing dot no namespace can parse.
        if (skipped.Length == 0)
        {
            return _baseNamespace;
        }

        return string.IsNullOrEmpty(_baseNamespace)
            ? string.Join('.', skipped)
            : $"{_baseNamespace}.{string.Join('.', skipped)}";
    }

    /// <summary>
    /// Determines whether a type is one the generator should mirror into Contracts rather than assume already
    /// exists there.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a Core-owned shared type.</returns>
    static bool IsCandidate(Type type)
    {
        var @namespace = type.Namespace ?? string.Empty;
        var isChronicleOwned = @namespace == _chronicleRootNamespace || @namespace.StartsWith($"{_chronicleRootNamespace}.", StringComparison.Ordinal);
        var isAlreadyAContract = @namespace == _baseNamespace || @namespace.StartsWith($"{_baseNamespace}.", StringComparison.Ordinal);

        return isChronicleOwned &&
            !isAlreadyAContract &&
            !type.IsInterface &&
            !type.IsGenericType &&
            !TypeHelper.IsReadModelType(type) &&
            !TypeHelper.IsConceptType(type) &&
            !TypeHelper.IsOneOfType(type) &&
            (type.IsEnum || (type.IsClass && !type.IsAbstract));
    }
}
