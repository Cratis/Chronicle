// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.ProxyGenerator.Templates;

/// <summary>
/// Describes the value on an enum.
/// </summary>
/// <param name="Name">Name of value.</param>
/// <param name="Value">The actual value.</param>
public record EnumValueDescriptor(string Name, int Value);
