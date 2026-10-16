// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// The exception thrown when prepared receipt facts do not match the atomic write they describe.
/// </summary>
/// <param name="reason">The violated infrastructure invariant.</param>
public class InvalidEffectReceipt(string reason) : Exception(reason);
