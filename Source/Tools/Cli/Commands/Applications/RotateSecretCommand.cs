// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Cli.Commands.Applications;

/// <summary>
/// Rotates the client secret for an application (OAuth client).
/// </summary>
public class RotateSecretCommand : ChronicleCommand<RotateSecretSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, RotateSecretSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        await services.Applications.ChangeSecret(new ChangeApplicationSecret
        {
            Id = settings.AppId,
            ClientSecret = settings.NewSecret
        });

        OutputFormatter.WriteMessage(format, $"Secret rotated for application '{settings.AppId}'.");
        return ExitCodes.Success;
    }
}
