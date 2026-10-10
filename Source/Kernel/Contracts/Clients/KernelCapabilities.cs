// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Clients;

/// <summary>
/// Identifies optional kernel behaviors advertised during connection.
/// </summary>
public static class KernelCapabilities
{
    /// <summary>
    /// The kernel accepts precise schemas for defaulted event properties without changing their generation.
    /// </summary>
    public const string PreciseEventTypeSchemas = "precise-event-type-schemas";
}
