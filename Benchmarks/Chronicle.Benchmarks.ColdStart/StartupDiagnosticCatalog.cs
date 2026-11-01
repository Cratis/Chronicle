// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Benchmarks.ColdStart;

/// <summary>
/// Explicit artifact catalog for the diagnostic child. The manifest is captured/asserted in the host, never by discovery in the explicit child.
/// Resolution and catalog construction are inside that child's readiness/allocation boundary.
/// </summary>
/// <param name="facets">The artifact types per facet.</param>
internal sealed class StartupDiagnosticCatalog(Dictionary<string, Type[]> facets) : IClientArtifactsProvider
{
    /// <summary>
    /// The artifact facet properties of <see cref="IClientArtifactsProvider"/>.
    /// </summary>
    internal static readonly PropertyInfo[] Facets = typeof(IClientArtifactsProvider).GetProperties();

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes => facets[nameof(EventTypes)];

    /// <inheritdoc/>
    public IEnumerable<Type> Projections => facets[nameof(Projections)];

    /// <inheritdoc/>
    public IEnumerable<Type> ModelBoundProjections => facets[nameof(ModelBoundProjections)];

    /// <inheritdoc/>
    public IEnumerable<Type> Reactors => facets[nameof(Reactors)];

    /// <inheritdoc/>
    public IEnumerable<Type> ReadModelReactors => facets[nameof(ReadModelReactors)];

    /// <inheritdoc/>
    public IEnumerable<Type> Reducers => facets[nameof(Reducers)];

    /// <inheritdoc/>
    public IEnumerable<Type> ReactorMiddlewares => facets[nameof(ReactorMiddlewares)];

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders => facets[nameof(ComplianceForTypesProviders)];

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders => facets[nameof(ComplianceForPropertiesProviders)];

    /// <inheritdoc/>
    public IEnumerable<Type> SecurityForTypesProviders => facets[nameof(SecurityForTypesProviders)];

    /// <inheritdoc/>
    public IEnumerable<Type> SecurityForPropertiesProviders => facets[nameof(SecurityForPropertiesProviders)];

    /// <inheritdoc/>
    public IEnumerable<Type> AdditionalEventInformationProviders => facets[nameof(AdditionalEventInformationProviders)];

    /// <inheritdoc/>
    public IEnumerable<Type> ConstraintTypes => facets[nameof(ConstraintTypes)];

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueConstraints => facets[nameof(UniqueConstraints)];

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueEventTypeConstraints => facets[nameof(UniqueEventTypeConstraints)];

    /// <inheritdoc/>
    public IEnumerable<Type> RemoveConstraintEventTypes => facets[nameof(RemoveConstraintEventTypes)];

    /// <inheritdoc/>
    public IEnumerable<Type> ClosesStreamEventTypes => facets[nameof(ClosesStreamEventTypes)];

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypeMigrators => facets[nameof(EventTypeMigrators)];

    /// <inheritdoc/>
    public IEnumerable<Type> EventSeeders => facets[nameof(EventSeeders)];

    /// <inheritdoc/>
    public IEnumerable<Type> EventSources => facets[nameof(EventSources)];

    /// <inheritdoc/>
    public IEnumerable<Type> Triggers => facets[nameof(Triggers)];

    /// <inheritdoc/>
    public IEnumerable<Type> OccurrenceHandlers => facets[nameof(OccurrenceHandlers)];

    /// <summary>
    /// Captures the assembly-qualified type names of every facet of a provider.
    /// </summary>
    /// <param name="provider">The provider to capture.</param>
    /// <returns>The manifest keyed by facet name.</returns>
    internal static Dictionary<string, string[]> Capture(IClientArtifactsProvider provider)
    {
        var result = new Dictionary<string, string[]>();
        foreach (var facet in Facets)
        {
            // Invoke now: the first getter initializes ALL default-provider facets.
            var types = (IEnumerable<Type>)facet.GetValue(provider)!;
            result.Add(facet.Name, types.Select(type => type.AssemblyQualifiedName!).ToArray());
        }
        return result;
    }

    /// <summary>
    /// Resolves an explicit artifact catalog from a manifest.
    /// </summary>
    /// <param name="manifest">The manifest keyed by facet name.</param>
    /// <returns>The explicit artifact catalog.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest facets do not match the provider facets.</exception>
    internal static ExplicitArtifactCatalog Resolve(Dictionary<string, string[]> manifest)
    {
        if (!Facets.Select(facet => facet.Name).Order().SequenceEqual(manifest.Keys.Order()))
        {
            throw new InvalidOperationException("Diagnostic catalog facet mismatch.");
        }
        var types = manifest.ToDictionary(entry => entry.Key, entry => entry.Value.Select(name => Type.GetType(name, throwOnError: true)!).ToArray());
        return new(new StartupDiagnosticCatalog(types), new ExplicitArtifacts().CreateSnapshot());
    }

    /// <summary>
    /// Asserts that the explicit catalog has the same identity and order as the automatic provider.
    /// </summary>
    /// <param name="automatic">The automatically discovered provider.</param>
    /// <param name="catalog">The explicit catalog.</param>
    /// <exception cref="InvalidOperationException">Thrown when the catalog differs in identity, order or declarations from the automatic provider.</exception>
    internal static void AssertParity(IClientArtifactsProvider automatic, ExplicitArtifactCatalog catalog)
    {
        var expected = Capture(automatic);
        var actual = Capture(catalog);
        foreach (var facet in expected)
        {
            if (!facet.Value.SequenceEqual(actual[facet.Key]))
            {
                throw new InvalidOperationException($"Diagnostic catalog identity/order mismatch: {facet.Key}.");
            }
        }
        foreach (var type in automatic.EventTypes)
        {
            if (EventTypeDescriptor.FromDeclaration(type, ColdStartKernel.EventStoreName) != catalog.GetEventDescriptorFor(type, ColdStartKernel.EventStoreName))
            {
                throw new InvalidOperationException($"Diagnostic event declaration mismatch: {type}.");
            }
        }
        foreach (var type in automatic.Reactors)
        {
            var expectedId = ArtifactIdentity.ObserverIdFor(type, type.GetCustomAttribute<ReactorAttribute>()?.Id.Value);
            if (catalog.GetReactorIdFor(type).Value != expectedId) throw new InvalidOperationException($"Diagnostic reactor identity mismatch: {type}.");
        }
        foreach (var type in automatic.Reducers)
        {
            var expectedId = ArtifactIdentity.ObserverIdFor(type, type.GetCustomAttribute<ReducerAttribute>()?.Id.Value);
            if (catalog.GetReducerIdFor(type).Value != expectedId) throw new InvalidOperationException($"Diagnostic reducer identity mismatch: {type}.");
        }
        foreach (var type in automatic.EventSources)
        {
            if (catalog.GetEventSourceNameFor(type) != ArtifactIdentity.EventSourceNameFor(type)) throw new InvalidOperationException($"Diagnostic event source identity mismatch: {type}.");
        }
    }
}
