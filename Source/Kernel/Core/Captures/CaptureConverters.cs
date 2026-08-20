// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;
using Capture = Cratis.Chronicle.Concepts.Captures.Capture;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Provides extension methods for converting between Kernel and contract capture representations.
/// </summary>
public static class CaptureConverters
{
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

    /// <summary>
    /// Converts stored captures into the read model the capture queries answer with.
    /// </summary>
    /// <param name="captures">The stored captures.</param>
    /// <returns>The captures as read models.</returns>
    internal static IEnumerable<CaptureDetails> ToReadModel(this IEnumerable<Capture> captures) =>
        [.. captures.Select(ToReadModel)];

    /// <summary>
    /// Converts a stored capture into the read model the capture queries answer with.
    /// </summary>
    /// <param name="capture">The stored capture.</param>
    /// <returns>The capture as a read model.</returns>
    internal static CaptureDetails ToReadModel(this Capture capture) =>
        new(
            capture.Id.ToString(),
            capture.Name,
            capture.Declaration,
            (Contracts.Captures.CaptureStatus)capture.Status);

    /// <summary>
    /// Converts declaration compilation errors into their contract representation.
    /// </summary>
    /// <param name="errors">The compilation errors.</param>
    /// <returns>The errors as validation messages.</returns>
    internal static IList<Contracts.Captures.CaptureValidationMessage> ToContract(IEnumerable<Engine.DeclarationLanguage.CompilerError> errors) =>
        [.. errors.Select(error => new Contracts.Captures.CaptureValidationMessage
        {
            Message = error.Message,
            Line = error.Line,
            Column = error.Column
        })];

    /// <summary>
    /// Resolves the identifier a capture is saved under, minting one when none was supplied.
    /// </summary>
    /// <param name="id">The supplied identifier.</param>
    /// <returns>The capture identifier.</returns>
    internal static Concepts.Captures.CaptureId ResolveCaptureId(string id) =>
        Guid.TryParse(id, out var guid) && guid != Guid.Empty ? new Concepts.Captures.CaptureId(guid) : Concepts.Captures.CaptureId.New();
}
