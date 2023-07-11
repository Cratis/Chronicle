// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic;

/// <summary>
/// Exception that gets thrown when a <see cref="IPropertyPathSegment">segment</see> within a <see cref="PropertyPath"/> is not an ExpandoObject.
/// </summary>
public class SegmentValueIsNotCollection : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentValueIsNotCollection"/> class.
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/>.</param>
    /// <param name="segment"><see cref="IPropertyPathSegment"/>.</param>
    public SegmentValueIsNotCollection(PropertyPath propertyPath, IPropertyPathSegment segment) : base($"Segment '{segment.Value}' in '{propertyPath}' is not a collection of ExpandoObject")
    {
    }
}
