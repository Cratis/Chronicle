// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies the winner of the receipt write.
/// </summary>
public enum EffectReceiptStatus
{
    /// <summary>
    /// This call durably recorded the receipt.
    /// </summary>
    Recorded = 0,

    /// <summary>
    /// A previous call recorded the returned receipt.
    /// </summary>
    AlreadyRecorded = 1
}
