// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the command for validating a capture declaration without saving it.
/// </summary>
/// <param name="EventStore">The event store the declaration is validated against.</param>
/// <param name="Declaration">The capture declaration language source text.</param>
/// <remarks>
/// This is what an editor calls as somebody types, so the answer is always messages rather than a failure.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.Captures)]
public record ValidateCaptureDeclaration(EventStoreName EventStore, string Declaration)
{
    /// <summary>
    /// Handles the command by compiling the declaration and validating what it describes.
    /// </summary>
    /// <param name="languageService">The <see cref="ILanguageService"/> to compile the declaration with.</param>
    /// <param name="captureValidator">The <see cref="ICaptureValidator"/> to validate the compiled capture with.</param>
    /// <returns>What compiling and validating had to say.</returns>
    public async Task<ValidateCaptureDeclarationResult> Handle(ILanguageService languageService, ICaptureValidator captureValidator)
    {
        var compilation = languageService.Compile(Declaration);
        var messages = await compilation.Match(
            definition => captureValidator.Validate(EventStore, definition),
            errors => Task.FromResult(errors.Errors.Select(error => new CaptureValidationMessage(error.Message, error.Line, error.Column))));

        return new(messages.ToContract());
    }
}
