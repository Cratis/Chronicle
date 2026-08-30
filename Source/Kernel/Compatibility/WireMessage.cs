// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Represents one message in the wire contract.
/// </summary>
/// <param name="FullName">The fully qualified message name, including its proto package.</param>
/// <param name="Fields">The fields it declares, keyed by field number.</param>
public record WireMessage(string FullName, IReadOnlyDictionary<int, WireField> Fields);
