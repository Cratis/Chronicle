// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a refusal-only or borrowed frame tries to complete a unit,
/// or a caller tries to complete or replace a unit owned by an immediate-append refusal frame.
/// </summary>
public class ImmediateAppendRefusalRequiresOwner() : Exception("This caller does not own completion of the unit of work. A refusal-only frame leaves completion with the existing owner; borrowed frames cannot complete the unit, replace it, or end refusal.");
