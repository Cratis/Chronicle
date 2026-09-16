// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Captures a persisted causation entry in an append receipt.
/// </summary>
/// <param name="Occurred">When the cause occurred.</param>
/// <param name="Type">The type of cause.</param>
/// <param name="Properties">The recorded properties of the cause.</param>
public record AppendCausation(DateTimeOffset Occurred, string Type, IDictionary<string, string> Properties)
{
    /// <summary>
    /// Takes a snapshot of persisted causation metadata.
    /// </summary>
    /// <param name="causation">The persisted cause.</param>
    /// <returns>The receipt entry.</returns>
    internal static AppendCausation From(Concepts.Auditing.Causation causation) =>
        new(causation.Occurred, causation.Type, new Dictionary<string, string>(causation.Properties));
}
