// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Confidentiality;

/// <summary>
/// Exception that gets thrown when where is no <see cref="SecurityMetadata"/> associated with a property.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NoSecurityMetadataForProperty"/> class.
/// </remarks>
/// <param name="property"><see cref="PropertyInfo"/> that does not have security metadata.</param>
public class NoSecurityMetadataForProperty(PropertyInfo property) : Exception($"Property '{property.Name}' on type '{property.DeclaringType?.FullName}' does not have any security metadata.");
