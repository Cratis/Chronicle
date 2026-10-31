// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Reads the operational opt-in flag.
/// </summary>
public interface IGenerationEnablement
{
    /// <summary>
    /// Gets whether generation initialization is enabled.
    /// </summary>
    bool Enabled { get; }
}
