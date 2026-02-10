// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Settings;

namespace Cratis.Chronicle.Grains.Settings;

/// <summary>
/// Defines the settings grain.
/// </summary>
public interface ISettings : IGrainWithStringKey
{
    /// <summary>
    /// Set the language model provider.
    /// </summary>
    /// <param name="provider">The <see cref="LanguageModelProvider"/> to set.</param>
    /// <returns>Awaitable task.</returns>
    Task SetLanguageModelProvider(LanguageModelProvider provider);

    /// <summary>
    /// Get the language model provider.
    /// </summary>
    /// <returns>The configured <see cref="LanguageModelProvider"/>.</returns>
    Task<LanguageModelProvider> GetLanguageModelProvider();
}
