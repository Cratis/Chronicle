// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to indicate that an event property should be populated from capture context.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="MapFromContextAttribute"/>.
/// </remarks>
/// <param name="contextProperty">The context property to map from.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class MapFromContextAttribute(string contextProperty) : Attribute
{
    /// <summary>
    /// Gets the context property to map from.
    /// </summary>
    public string ContextProperty { get; } = contextProperty;
}
