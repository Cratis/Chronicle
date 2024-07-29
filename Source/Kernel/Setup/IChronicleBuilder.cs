// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Defines a builder for configuring Chronicle.
/// </summary>
public interface IChronicleBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> for the builder.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the <see cref="IConfiguration"/> for the builder.
    /// </summary>
    IConfiguration Configuration { get; }
}
