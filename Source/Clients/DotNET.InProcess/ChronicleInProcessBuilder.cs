// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.InProcess;

/// <summary>
/// Represents an implementation of <see cref="IChronicleInProcessBuilder"/> that wraps the internal kernel builder.
/// </summary>
/// <param name="inner">The inner <see cref="Cratis.Chronicle.Configuration.IChronicleBuilder"/> to wrap.</param>
internal class ChronicleInProcessBuilder(Cratis.Chronicle.Configuration.IChronicleBuilder inner) : IChronicleInProcessBuilder
{
    /// <inheritdoc/>
    public ISiloBuilder SiloBuilder => inner.SiloBuilder;

    /// <inheritdoc/>
    public IServiceCollection Services => inner.Services;

    /// <inheritdoc/>
    public IConfiguration Configuration => inner.Configuration;
}
