// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when an effect receipt key, outcome or wire response is inconsistent.
/// </summary>
/// <param name="reason">The violated receipt invariant.</param>
public class EffectReceiptNotValid(string reason) : Exception($"Invalid effect receipt: {reason}");
