// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle;

/// <summary>
/// Defines additional client service registrations, discovered from the type universe when the Chronicle client is added.
/// </summary>
/// <remarks>
/// Implementations must have a public parameterless constructor. Registrations apply only to the client container,
/// not to the kernel or to the private containers used by testing.
/// </remarks>
public interface IChronicleClientServices
{
    /// <summary>
    /// Add services to the client container.
    /// </summary>
    /// <param name="services">The client service collection.</param>
    void Configure(IServiceCollection services);
}
