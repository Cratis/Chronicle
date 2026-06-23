// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Marks a <see cref="IReadModelReactor"/> to watch through the materialized read model API rather than the
/// change-stream API.
/// </summary>
/// <remarks>
/// When applied, the reactor observes the materialized window of instances and the change type is deduced by
/// comparing successive windows by key and last handled event sequence number.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class MaterializedAttribute : Attribute;
