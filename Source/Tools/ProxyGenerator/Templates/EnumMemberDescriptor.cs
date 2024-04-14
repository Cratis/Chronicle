// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.ProxyGenerator.Templates;

/// <summary>
/// Describes the value on an enum.
/// </summary>
/// <param name="Name">Name of value.</param>
/// <param name="Value">The actual value.</param>
public record EnumMemberDescriptor(string Name, object Value);
