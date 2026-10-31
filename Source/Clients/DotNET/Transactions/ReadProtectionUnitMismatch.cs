// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when an invocation tries to use a different unit of work.
/// </summary>
public class ReadProtectionUnitMismatch() : Exception("Read protection belongs to the invocation's unit of work. Do not replace it or enroll dependencies into another unit; explicitly declare advisory reads instead.");
