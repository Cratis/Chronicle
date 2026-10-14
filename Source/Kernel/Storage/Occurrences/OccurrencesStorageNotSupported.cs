// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// The exception thrown when a custom provider does not support occurrence infrastructure storage.
/// </summary>
public class OccurrencesStorageNotSupported() : Exception("The storage provider does not support durable application occurrences and schedules.");
