// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Internal interface for getting the services available.
/// </summary>
internal interface IChronicleServicesAccessor
{
    /// <summary>
    /// Gets the available services.
    /// </summary>
    IServices Services { get; }
}
