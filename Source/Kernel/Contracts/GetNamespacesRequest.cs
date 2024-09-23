// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Represents the request for getting namespaces.
/// </summary>
/// <param name="EventStore">Event store to get for.</param>
public record GetNamespacesRequest(string EventStore);
