// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.ProxyGenerator.Templates;

/// <summary>
/// Describes a query argument for template purposes.
/// </summary>
/// <param name="OriginalType">The original type of the argument.</param>
/// <param name="Name">Name of argument.</param>
/// <param name="Type">Type of argument.</param>
/// <param name="IsOptional">Whether or not the argument is nullable / optional.</param>
public record RequestArgumentDescriptor(
    Type OriginalType,
    string Name,
    string Type,
    bool IsOptional);
