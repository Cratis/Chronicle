// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.ProxyGenerator.Templates;

/// <summary>
/// Describes a property for templating purposes.
/// </summary>
/// <param name="Name">Name of the property.</param>
/// <param name="Type">Type of the property.</param>
/// <param name="Constructor">The JavaScript constructor for the type.</param>
/// <param name="IsEnumerable">Whether or not the property is an enumerable or not.</param>
/// <param name="IsNullable">Whether or not the property is nullable or not.</param>
/// <param name="HasDerivatives">Whether or not the property type has derivatives, typically if it is an interface type.</param>
/// <param name="Derivatives">Optionally any derivatives of the type.</param>
public record PropertyDescriptor(string Name, string Type, string Constructor, bool IsEnumerable, bool IsNullable, bool HasDerivatives, IEnumerable<string>? Derivatives = default);
