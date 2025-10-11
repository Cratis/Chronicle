// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Keys;

/// <summary>
/// Represents metadata for defining which property use as key, or marks a property as a key.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class KeyAttribute : Attribute;
