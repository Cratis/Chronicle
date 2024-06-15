// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Models;

/// <summary>
/// Attribute that is used to provide metadata about a property or parameter being the <see cref="ModelKey"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ModelKeyAttribute : Attribute;
