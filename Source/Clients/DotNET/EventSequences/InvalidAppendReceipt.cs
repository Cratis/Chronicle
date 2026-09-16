// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when an acknowledged append has missing or inconsistent persisted metadata.
/// The append may already be persisted; do not retry without reconciling its outcome.
/// </summary>
/// <param name="reason">The receipt validation failure.</param>
public class InvalidAppendReceipt(string reason) : Exception($"The append was acknowledged and may already be persisted, but its receipt is invalid: {reason}. Reconcile the outcome before retrying.");
