// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Engines.Sinks.InMemory;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> for <see cref="InMemorySink"/>.
/// </summary>
public class InMemorySinkFactory : ISinkFactory
{
    readonly ITypeFormats _typeFormats;
    readonly IObjectComparer _comparer;

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemory;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySinkFactory"/> class.
    /// </summary>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for resolving actual types from JSON schema.</param>
    /// <param name="comparer"><see cref="IObjectComparer"/> used for complex comparisons of objects.</param>
    public InMemorySinkFactory(ITypeFormats typeFormats, IObjectComparer comparer)
    {
        _typeFormats = typeFormats;
        _comparer = comparer;
    }

    /// <inheritdoc/>
    public ISink CreateFor(Model model) => new InMemorySink(model, _typeFormats, _comparer);
}
