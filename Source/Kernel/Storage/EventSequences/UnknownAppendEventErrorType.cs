// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="AppendEventError"/>.
/// </summary>
/// <param name="errorType">The unknown <see cref="AppendEventError"/>.</param>
public class UnknownAppendEventErrorType(AppendEventError errorType) : Exception($"Unknown append event error type {errorType} occurred");
