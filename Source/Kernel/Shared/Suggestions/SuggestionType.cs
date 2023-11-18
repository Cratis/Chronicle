// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Suggestions;

/// <summary>
/// Represents type of a suggestion.
/// </summary>
/// <param name="Value">String representation of the job type.</param>
/// <remarks>
/// The expected format is <c>Namespace.Type</c>.
/// </remarks>
public record SuggestionType(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The <see cref="SuggestionType"/> for when it is not set.
    /// </summary>
    public static readonly SuggestionType NotSet = new("Undefined");

    /// <summary>
    /// Implicitly convert from <see cref="Type"/> to <see cref="SuggestionType"/>.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to convert from.</param>
    public static implicit operator SuggestionType(Type type) => new(type.AssemblyQualifiedName ?? type.Name);

    /// <summary>
    /// Implicitly convert from <see cref="SuggestionType"/> to <see cref="Type"/>.
    /// </summary>
    /// <param name="type"><see cref="SuggestionType"/> to convert from.</param>
    public static implicit operator Type(SuggestionType type) => Type.GetType(type.Value) ?? throw new UnknownClrTypeForSuggestionType(type);
}
