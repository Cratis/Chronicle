// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the outcome of completing a stream.
/// </summary>
/// <param name="IsSuccess">Whether the stream was completed successfully by this call.</param>
/// <param name="SequenceNumber">The tail sequence number captured at the time of completion. Only meaningful when <paramref name="IsSuccess"/> is <see langword="true"/>.</param>
/// <param name="Error">The error encountered when <paramref name="IsSuccess"/> is <see langword="false"/>, or <see cref="CompleteStreamError.None"/> otherwise.</param>
public record CompleteStreamOutcome(bool IsSuccess, EventSequenceNumber SequenceNumber, CompleteStreamError Error);
