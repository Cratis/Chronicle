// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.Engines.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSinkFactory"/> that creates <see cref="NullProjectionSink"/>.
/// </summary>
public class NullProjectionSinkFactory : IProjectionSinkFactory
{
    static readonly NullProjectionSink _instance = new();

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => ProjectionSinkTypeId.Null;

    /// <inheritdoc/>
    public IProjectionSink CreateFor(Model model) => _instance;
}
