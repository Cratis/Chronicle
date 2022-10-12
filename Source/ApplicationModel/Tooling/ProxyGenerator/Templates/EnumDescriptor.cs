// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.ProxyGenerator.Templates;

/// <summary>
/// Describes an enum.
/// </summary>
/// <param name="Name">Name of the enum,</param>
/// <param name="Values">The values on the enum.</param>
public record EnumDescriptor(string Name, IEnumerable<EnumValueDescriptor> Values);
