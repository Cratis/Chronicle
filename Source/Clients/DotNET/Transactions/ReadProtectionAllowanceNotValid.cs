// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when an allowance is unknown or has no explicit reason.
/// </summary>
public class ReadProtectionAllowanceNotValid() : Exception("A read allowance must be Advisory or Contributed and must name an explicit reason.");
