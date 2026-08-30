// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Represents one enum in the wire contract.
/// </summary>
/// <param name="FullName">The fully qualified enum name, including its proto package.</param>
/// <param name="Values">The value names it declares, keyed by value number.</param>
public record WireEnum(string FullName, IReadOnlyDictionary<int, string> Values);
