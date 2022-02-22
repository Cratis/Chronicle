// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties;

/// <summary>
/// Defines a segment within a <see cref="PropertyPath"/>.
/// </summary>
public interface IPropertyPathSegment
{
    /// <summary>
    /// Gets the value that represents the segment.
    /// </summary>
    string Value { get; }
}
