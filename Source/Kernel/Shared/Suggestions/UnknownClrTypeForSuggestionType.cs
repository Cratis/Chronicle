// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Suggestions;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="SuggestionType"/> is encountered.
/// </summary>
public class UnknownClrTypeForSuggestionType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownClrTypeForSuggestionType"/> class.
    /// </summary>
    /// <param name="type"><see cref="SuggestionType"/> that has an invalid type identifier.</param>
    public UnknownClrTypeForSuggestionType(SuggestionType type)
        : base($"Unknown operation type '{type}'")
    {
    }
}
