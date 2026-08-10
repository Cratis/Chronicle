// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents the property and comparand a join filters its update on.
/// </summary>
/// <param name="Property">The MongoDB property the filter compares.</param>
/// <param name="Value">The <see cref="BsonValue"/> the property is compared against. <see cref="BsonNull.Value"/> means the join has no key to filter on.</param>
internal record JoinFilterTarget(string Property, BsonValue Value);
