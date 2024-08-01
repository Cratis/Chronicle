// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.InMemory;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> for <see cref="InMemorySink"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemorySinkFactory"/> class.
/// </remarks>
/// <param name="typeFormats">The <see cref="ITypeFormats"/> for resolving actual types from JSON schema.</param>
public class InMemorySinkFactory(ITypeFormats typeFormats) : ISinkFactory
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemory;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, Model model) => new InMemorySink(model, typeFormats);
}
