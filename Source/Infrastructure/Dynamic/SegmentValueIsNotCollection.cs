// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Properties;

namespace Cratis.Dynamic;

/// <summary>
/// Exception that gets thrown when a <see cref="IPropertyPathSegment">segment</see> within a <see cref="PropertyPath"/> is not an ExpandoObject.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SegmentValueIsNotCollection"/> class.
/// </remarks>
/// <param name="propertyPath"><see cref="PropertyPath"/>.</param>
/// <param name="segment"><see cref="IPropertyPathSegment"/>.</param>
public class SegmentValueIsNotCollection(PropertyPath propertyPath, IPropertyPathSegment segment) : Exception($"Segment '{segment.Value}' in '{propertyPath}' is not a collection of ExpandoObject")
{
}
