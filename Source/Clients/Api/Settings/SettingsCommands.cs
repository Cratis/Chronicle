// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Settings;

namespace Cratis.Chronicle.Api.Settings;

/// <summary>
/// Represents the API for commands related to settings.
/// </summary>
[Route("/api/settings")]
public class SettingsCommands : ControllerBase
{
    readonly ISettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsCommands"/> class.
    /// </summary>
    /// <param name="settings">The <see cref="ISettings"/> contract.</param>
    internal SettingsCommands(ISettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Configure a language model.
    /// </summary>
    /// <param name="command"><see cref="ConfigureLanguageModelCommand"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("configure-language-model")]
    public Task ConfigureLanguageModel([FromBody] ConfigureLanguageModelCommand command) =>
        _settings.ConfigureLanguageModel(new()
        {
            Endpoint = command.Endpoint,
            ApiKey = command.ApiKey,
            Model = command.Model
        });
}
