// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.ProxyGenerator.Templates;

/// <summary>
/// Describes a property for templating purposes.
/// </summary>
/// <param name="OriginalType">The original type of the property.</param>
/// <param name="Name">Name of the property.</param>
/// <param name="Type">Type of the property.</param>
/// <param name="Constructor">The JavaScript constructor for the type.</param>
/// <param name="IsEnumerable">Whether or not the property is an enumerable or not.</param>
/// <param name="IsNullable">Whether or not the property is nullable or not.</param>
public record PropertyDescriptor(
    Type OriginalType,
    string Name,
    string Type,
    string Constructor,
    bool IsEnumerable,
    bool IsNullable);
