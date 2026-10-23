// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines the fallback when no silo advertises the Jobs role.
/// </summary>
public enum JobsFallback
{
    /// <summary>
    /// Allow job steps on any compatible silo.
    /// </summary>
    AnySilo = 0,

    /// <summary>
    /// Wait for a job worker silo. Reserved until durable waiting is supported.
    /// </summary>
    Wait = 1
}
