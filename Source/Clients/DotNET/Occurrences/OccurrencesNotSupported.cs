// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when occurrence support is absent or unknown.
/// </summary>
public class OccurrencesNotSupported() : Exception("The connected kernel has not advertised occurrence support. No occurrence operation was sent.");
