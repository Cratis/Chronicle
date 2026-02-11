// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Settings;

/// <summary>
/// Defines the contract for working with settings.
/// </summary>
[Service]
public interface ISettings
{
    /// <summary>
    /// Configure a language model.
    /// </summary>
    /// <param name="command">The configuration command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task ConfigureLanguageModel(ConfigureLanguageModel command);
}
