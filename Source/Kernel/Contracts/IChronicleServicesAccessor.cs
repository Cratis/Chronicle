// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Internal interface for getting the services available.
/// </summary>
public interface IChronicleServicesAccessor
{
    /// <summary>
    /// Gets the available services.
    /// </summary>
    IServices Services { get; }
}
