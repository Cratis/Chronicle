// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// The exception that is thrown when a reducer contains an unsupported IL operand.
/// </summary>
/// <param name="operand">The unsupported operand.</param>
public class UnsupportedReducerFingerprintOperand(string operand) : Exception($"Cannot fingerprint reducer IL operand '{operand}'.");
