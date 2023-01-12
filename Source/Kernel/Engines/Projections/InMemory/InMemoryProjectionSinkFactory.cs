// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.Engines.Projections.InMemory;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSinkFactory"/> for <see cref="InMemoryProjectionSink"/>.
/// </summary>
public class InMemoryProjectionSinkFactory : IProjectionSinkFactory
{
    readonly ITypeFormats _typeFormats;
    readonly IObjectsComparer _comparer;

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => WellKnownProjectionSinkTypes.InMemory;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryProjectionSinkFactory"/> class.
    /// </summary>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for resolving actual types from JSON schema.</param>
    /// <param name="comparer"><see cref="IObjectsComparer"/> used for complex comparisons of objects.</param>
    public InMemoryProjectionSinkFactory(ITypeFormats typeFormats, IObjectsComparer comparer)
    {
        _typeFormats = typeFormats;
        _comparer = comparer;
    }

    /// <inheritdoc/>
    public IProjectionSink CreateFor(Model model) => new InMemoryProjectionSink(model, _typeFormats, _comparer);
}
