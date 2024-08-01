// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Represents type of a recommendation.
/// </summary>
/// <param name="Value">String representation of the job type.</param>
/// <remarks>
/// The expected format is <c>Namespace.Type</c>.
/// </remarks>
public record RecommendationType(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The <see cref="RecommendationType"/> for when it is not set.
    /// </summary>
    public static readonly RecommendationType NotSet = new("Undefined");

    /// <summary>
    /// Implicitly convert from <see cref="Type"/> to <see cref="RecommendationType"/>.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to convert from.</param>
    public static implicit operator RecommendationType(Type type) => new(type.AssemblyQualifiedName ?? type.Name);

    /// <summary>
    /// Implicitly convert from <see cref="RecommendationType"/> to <see cref="Type"/>.
    /// </summary>
    /// <param name="type"><see cref="RecommendationType"/> to convert from.</param>
    public static implicit operator Type(RecommendationType type) => Type.GetType(type.Value) ?? throw new UnknownClrTypeForRecommendationType(type);
}
