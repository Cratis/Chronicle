// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Schemas;
using Aksio.Cratis.Sinks;
using Cratis.Chronicle.Projections;

namespace Aksio.Cratis.Kernel.Storage.Sinks.InMemory;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> for <see cref="InMemorySink"/>.
/// </summary>
public class InMemorySinkFactory : ISinkFactory
{
    readonly ITypeFormats _typeFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySinkFactory"/> class.
    /// </summary>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for resolving actual types from JSON schema.</param>
    public InMemorySinkFactory(ITypeFormats typeFormats)
    {
        _typeFormats = typeFormats;
    }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemory;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, Model model) => new InMemorySink(model, _typeFormats);
}
