// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to configure the property used as the capture key.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="CaptureKeyAttribute"/>.
/// </remarks>
/// <param name="property">The property path used as the key.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CaptureKeyAttribute(string property) : Attribute
{
    /// <summary>
    /// Gets the property path used as the key.
    /// </summary>
    public string Property { get; } = property;
}
