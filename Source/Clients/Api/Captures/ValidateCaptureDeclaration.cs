// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ICapturesService = Cratis.Chronicle.Contracts.Captures.ICaptures;

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents the command for validating a capture declaration - compiling it and verifying what it
/// references, such as external services and event types.
/// </summary>
/// <param name="EventStore">The event store the declaration targets.</param>
/// <param name="Declaration">The capture declaration language source text to validate.</param>
[Command]
public record ValidateCaptureDeclaration(string EventStore, string Declaration)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="captures">The <see cref="ICapturesService"/> contract.</param>
    /// <returns>Collection of <see cref="CaptureValidationMessage"/> - empty when the declaration is valid.</returns>
    internal async Task<IEnumerable<CaptureValidationMessage>> Handle(ICapturesService captures)
    {
        var response = await captures.ValidateDeclaration(new()
        {
            EventStore = EventStore,
            Declaration = Declaration
        });

        return response.Messages.ToApi();
    }
}
