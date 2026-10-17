// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when durable lifecycle work cannot be reconciled safely.
/// </summary>
/// <param name="message">The violated lifecycle invariant.</param>
public class LifecycleOccurrenceNotValid(string message) : Exception(message);
