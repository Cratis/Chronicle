// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services;

namespace Cratis.Chronicle.Compliance;

[WireContract]
public record ErasureWireRequest(string Identifier, string Namespace, string EventStore, string? RequestId = null);
