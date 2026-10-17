// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when a receipt response exceeds 64 KiB of UTF-8 JSON.
/// </summary>
public class EffectReceiptResponseTooLarge() : Exception("An effect receipt response must not exceed 64 KiB of UTF-8 JSON.");
