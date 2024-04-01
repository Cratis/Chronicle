// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.ProxyGenerator.Templates;

/// <summary>
/// Describes a query argument for template purposes.
/// </summary>
/// <param name="Name">Name of argument.</param>
/// <param name="Type">Type of argument.</param>
/// <param name="IsOptional">Whether or not the argument is nullable / optional.</param>
public record RequestArgumentDescriptor(string Name, string Type, bool IsOptional);
