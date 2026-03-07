// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Sys;

/// <summary>
/// Represents the state for the <see cref="ISystem"/> grain.
/// </summary>
public class SystemInformationState
{
    /// <summary>
    /// Gets an empty <see cref="SystemInformationState"/>.
    /// </summary>
    public static readonly SystemInformationState Empty = new();

    /// <summary>
    /// Gets or sets the semantic version.
    /// </summary>
    public SemanticVersion? Version { get; set; }
}
