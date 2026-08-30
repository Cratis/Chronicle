// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grpc;

/// <summary>
/// Attribute used on a method of a <see cref="KeyedByAttribute{TKey}"/>-decorated grain interface to expose it
/// as a query on the service the interface belongs to.
/// </summary>
/// <remarks>
/// Reserved for a grain-state read with no storage-layer or read-model equivalent - most lookups belong on a
/// <c>[ReadModel]</c> as an ordinary static query method instead, the same way every other query in a service is
/// derived. This exists for the remainder that only a live grain can answer.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryAttribute : Attribute;
