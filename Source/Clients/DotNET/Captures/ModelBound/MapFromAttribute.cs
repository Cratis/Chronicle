// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to indicate that an event property should be populated from a capture source path.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="MapFromAttribute"/>.
/// </remarks>
/// <param name="sourcePath">The source path to map from.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class MapFromAttribute(string sourcePath) : Attribute
{
    /// <summary>
    /// Gets the source path to map from.
    /// </summary>
    public string SourcePath { get; } = sourcePath;
}
