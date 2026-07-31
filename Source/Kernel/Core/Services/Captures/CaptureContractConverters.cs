// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Services.Captures;

/// <summary>
/// Provides extension methods for converting between Kernel and contract capture representations.
/// </summary>
public static class CaptureContractConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Capture"/> to a contract <see cref="Contracts.Captures.Capture"/>.
    /// </summary>
    /// <param name="capture">The Kernel capture.</param>
    /// <returns>The contract capture.</returns>
    public static Contracts.Captures.Capture ToContract(this Capture capture) =>
        new()
        {
            Id = capture.Id.ToString(),
            Name = capture.Name,
            Declaration = capture.Declaration,
            Status = (Contracts.Captures.CaptureStatus)capture.Status
        };

    /// <summary>
    /// Converts a Kernel <see cref="CaptureValidationMessage"/> to a contract <see cref="Contracts.Captures.CaptureValidationMessage"/>.
    /// </summary>
    /// <param name="message">The Kernel message.</param>
    /// <returns>The contract message.</returns>
    public static Contracts.Captures.CaptureValidationMessage ToContract(this CaptureValidationMessage message) =>
        new()
        {
            Message = message.Message,
            Line = message.Line,
            Column = message.Column
        };

    /// <summary>
    /// Converts a collection of Kernel <see cref="CaptureValidationMessage">messages</see> to their contract representation.
    /// </summary>
    /// <param name="messages">The Kernel messages.</param>
    /// <returns>The contract messages.</returns>
    public static IList<Contracts.Captures.CaptureValidationMessage> ToContract(this IEnumerable<CaptureValidationMessage> messages) =>
        messages.Select(message => message.ToContract()).ToList();
}
