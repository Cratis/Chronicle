// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Events.Projections.InMemory;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSinkFactory"/> for <see cref="InMemoryProjectionSink"/>.
/// </summary>
public class InMemoryProjectionSinkFactory : IProjectionSinkFactory
{
    readonly ITypeFormats _typeFormats;

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => WellKnownProjectionSinkTypes.InMemory;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryProjectionSinkFactory"/> class.
    /// </summary>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for resolving actual types from JSON schema.</param>
    public InMemoryProjectionSinkFactory(ITypeFormats typeFormats)
    {
        _typeFormats = typeFormats;
    }

    /// <inheritdoc/>
    public IProjectionSink CreateFor(Model model) => new InMemoryProjectionSink(model, _typeFormats);
}
