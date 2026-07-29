// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Defines a system for working with external service registrations for the Kernel.
/// </summary>
public interface IExternalServices
{
    /// <summary>
    /// Registers an external service.
    /// </summary>
    /// <param name="name">The name of the external service. The name is also used as its identifier.</param>
    /// <param name="configure">The <see cref="Action{T}"/> for configuring the external service.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(string name, Action<IExternalServiceBuilder> configure);
}
