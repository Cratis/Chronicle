// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Settings;

namespace Cratis.Chronicle.Grains.Settings;

/// <summary>
/// Represents the state of settings.
/// </summary>
public class SettingsState
{
    /// <summary>
    /// Gets or sets the language model provider configuration.
    /// </summary>
    public LanguageModelProvider LanguageModelProvider { get; set; } = LanguageModelProvider.None;
}
