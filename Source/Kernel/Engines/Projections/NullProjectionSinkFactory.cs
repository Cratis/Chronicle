// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Engines.Projections;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> that creates <see cref="NullSink"/>.
/// </summary>
public class NullProjectionSinkFactory : ISinkFactory
{
    static readonly NullSink _instance = new();

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Null;

    /// <inheritdoc/>
    public ISink CreateFor(Model model) => _instance;
}
