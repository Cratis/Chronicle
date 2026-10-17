// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies a plaintext application trigger with a stable, deployment-independent identity.
/// </summary>
/// <param name="id">The stable trigger identifier.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TriggerAttribute(string id) : Attribute
{
    /// <summary>
    /// Gets the stable trigger identifier.
    /// </summary>
    public TriggerId Id { get; } = id;
}
