// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Determines who supplies the lifecycle activation identity.
/// </summary>
public enum ActivationScope
{
    /// <summary>
    /// The host supplies a logical activation shared across instances; stopping one instance does not end it.
    /// </summary>
    Application = 0,

    /// <summary>
    /// Each process start uses a fresh identity and attempts shutdown during ApplicationStopping.
    /// </summary>
    PerProcess = 1
}
