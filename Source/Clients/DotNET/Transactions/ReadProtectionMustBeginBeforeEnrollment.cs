// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when enforcement starts after events or dependencies were enrolled.
/// </summary>
public class ReadProtectionMustBeginBeforeEnrollment() : Exception("Read protection must begin before any event or read dependency is enrolled in the unit of work.");
