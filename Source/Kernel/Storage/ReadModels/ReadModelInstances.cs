// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.ReadModels;

/// <summary>
/// Represents a collection of read model instances along with the total count.
/// </summary>
/// <param name="Instances">The collection of read model instances.</param>
/// <param name="TotalCount">The total count of read model instances.</param>
public record ReadModelInstances(IEnumerable<ExpandoObject> Instances, long TotalCount);
