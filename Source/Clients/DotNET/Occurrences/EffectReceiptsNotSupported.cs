// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when atomic effect receipt support is absent or unknown.
/// </summary>
public class EffectReceiptsNotSupported() : Exception("The connected kernel or client implementation has not advertised atomic effect receipt support. No unreceipted append is permitted.");
