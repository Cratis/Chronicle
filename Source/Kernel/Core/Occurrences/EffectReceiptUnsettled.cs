// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when a receipt-only write cannot be settled; the caller may retry.
/// </summary>
public class EffectReceiptUnsettled() : Exception("EffectReceiptUnsettled");
