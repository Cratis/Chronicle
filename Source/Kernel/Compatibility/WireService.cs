// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Represents one gRPC service in the wire contract.
/// </summary>
/// <param name="FullName">The fully qualified service name, including its proto package.</param>
/// <param name="Methods">The methods it exposes, keyed by method name.</param>
public record WireService(string FullName, IReadOnlyDictionary<string, WireMethod> Methods);
