// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Models;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> that creates <see cref="NullSink"/>.
/// </summary>
public class NullProjectionSinkFactory : ISinkFactory
{
    static readonly NullSink _instance = new();

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Null;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, Model model) => _instance;
}
