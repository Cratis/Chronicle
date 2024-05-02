// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.ProxyGenerator;

/// <summary>
/// Represents a target type and optional import module.
/// </summary>
/// <param name="Type">Type.</param>
/// <param name="Constructor">The JavaScript constructor type.</param>
/// <param name="ImportFromModule">Module to import from. default or empty means no need to import.</param>
/// <param name="Final">Whether or not it absolutely is this type and do not try to resolve a more specific one.</param>
public record TargetType(string Type, string Constructor, string ImportFromModule = "", bool Final = false);
