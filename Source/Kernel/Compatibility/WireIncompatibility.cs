// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Represents one way in which a newer wire contract fails to serve an older one.
/// </summary>
/// <param name="Kind">What kind of incompatibility this is.</param>
/// <param name="Path">The contract element it is about, as a fully qualified proto path.</param>
/// <param name="Description">A sentence naming what changed, written for whoever has to fix it.</param>
public record WireIncompatibility(WireIncompatibilityKind Kind, string Path, string Description)
{
    /// <inheritdoc/>
    public override string ToString() => $"{Path}: {Description}";
}
