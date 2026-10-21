// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a second effect key is enrolled in the same unit of work.
/// </summary>
public class UnitOfWorkEffectReceiptAlreadySet() : Exception("A unit of work carries one effect key. Nested commands share the outer receipt.");
