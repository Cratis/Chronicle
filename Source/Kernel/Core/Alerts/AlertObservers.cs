// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Defines the identity convention for the kernel's own alert observers.
/// </summary>
public static class AlertObservers
{
    /// <summary>
    /// The prefix always excluded from alert evaluation to prevent alerts about alert delivery itself.
    /// </summary>
    /// <remarks>
    /// Future alert reactors must build their observer identifiers from this prefix, regardless of their namespace.
    /// Kernel reactors prepend $system. to their reactor identifier, so use the remainder for an explicit reactor ID.
    /// Other kernel observers are not excluded by this convention.
    /// </remarks>
    public const string Prefix = "$system.Cratis.Chronicle.Alerts.";
}
