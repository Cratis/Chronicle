// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.ProxyGenerator.Templates;

/// <summary>
/// Describes an enum.
/// </summary>
/// <param name="Type">Original type.</param>
/// <param name="Name">Name of the enum.</param>
/// <param name="Values">The values on the enum.</param>
/// <param name="TypesInvolved">Additional types involved.</param>
public record EnumDescriptor(
    Type Type,
    string Name,
    IEnumerable<EnumMemberDescriptor> Values,
    IEnumerable<Type> TypesInvolved) : IDescriptor;
