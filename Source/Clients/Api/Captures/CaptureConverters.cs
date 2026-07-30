// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Provides extension methods for converting between contract and API capture representations.
/// </summary>
internal static class CaptureConverters
{
    /// <summary>
    /// Converts a contract <see cref="Contracts.Captures.Capture"/> to an API <see cref="Capture"/>.
    /// </summary>
    /// <param name="capture">The contract capture.</param>
    /// <returns>The API capture.</returns>
    public static Capture ToApi(this Contracts.Captures.Capture capture) =>
        new()
        {
            Id = capture.Id,
            Name = capture.Name,
            Declaration = capture.Declaration,
            Status = (CaptureStatus)capture.Status
        };

    /// <summary>
    /// Converts a collection of contract <see cref="Contracts.Captures.Capture">captures</see> to their API representation.
    /// </summary>
    /// <param name="captures">The contract captures.</param>
    /// <returns>The API captures.</returns>
    public static IEnumerable<Capture> ToApi(this IEnumerable<Contracts.Captures.Capture> captures) =>
        captures.Select(capture => capture.ToApi()).ToArray();

    /// <summary>
    /// Converts a contract <see cref="Contracts.Captures.CaptureValidationMessage"/> to an API <see cref="CaptureValidationMessage"/>.
    /// </summary>
    /// <param name="message">The contract message.</param>
    /// <returns>The API message.</returns>
    public static CaptureValidationMessage ToApi(this Contracts.Captures.CaptureValidationMessage message) =>
        new()
        {
            Message = message.Message,
            Line = message.Line,
            Column = message.Column
        };

    /// <summary>
    /// Converts a collection of contract <see cref="Contracts.Captures.CaptureValidationMessage">messages</see> to their API representation.
    /// </summary>
    /// <param name="messages">The contract messages.</param>
    /// <returns>The API messages.</returns>
    public static IEnumerable<CaptureValidationMessage> ToApi(this IEnumerable<Contracts.Captures.CaptureValidationMessage> messages) =>
        messages.Select(message => message.ToApi()).ToArray();
}
