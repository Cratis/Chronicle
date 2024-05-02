// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.ProxyGenerator.Templates;

/// <summary>
/// Describes a type for templating purposes.
/// </summary>
/// <param name="Type">Original type.</param>
/// <param name="Name">Name of the type.</param>
/// <param name="Properties">Properties on the type.</param>
/// <param name="Imports">Additional import statements.</param>
/// <param name="TypesInvolved">Collection of types involved in the command.</param>
public record TypeDescriptor(
    Type Type,
    string Name,
    IEnumerable<PropertyDescriptor> Properties,
    IEnumerable<ImportStatement> Imports,
    IEnumerable<Type> TypesInvolved) : IDescriptor;
