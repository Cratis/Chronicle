// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Extension methods for working with <see cref="VariantOfAttribute{TIdentity}"/> and
/// <see cref="GlobalForAttribute{TIdentity}"/>.
/// </summary>
internal static class VariantOfExtensions
{
    /// <summary>
    /// Attempts to get the logical identity type a variant type is grouped under.
    /// </summary>
    /// <param name="type">The candidate variant type.</param>
    /// <param name="identityType">The logical identity type, when found.</param>
    /// <returns>True if the type is decorated with <see cref="VariantOfAttribute{TIdentity}"/>; otherwise, false.</returns>
    internal static bool TryGetVariantIdentity(this Type type, out Type identityType) =>
        type.TryGetIdentityFrom(typeof(VariantOfAttribute<>), out identityType);

    /// <summary>
    /// Attempts to get the logical identity type a shared handler type declares handlers for.
    /// </summary>
    /// <param name="type">The candidate shared handler type.</param>
    /// <param name="identityType">The logical identity type, when found.</param>
    /// <returns>True if the type is decorated with <see cref="GlobalForAttribute{TIdentity}"/>; otherwise, false.</returns>
    internal static bool TryGetGlobalForIdentity(this Type type, out Type identityType) =>
        type.TryGetIdentityFrom(typeof(GlobalForAttribute<>), out identityType);

    static bool TryGetIdentityFrom(this Type type, Type attributeDefinition, out Type identityType)
    {
        var attribute = type.GetCustomAttributes(inherit: false)
            .Cast<Attribute>()
            .FirstOrDefault(attr => attr.GetType().IsGenericType &&
                                    attr.GetType().GetGenericTypeDefinition() == attributeDefinition);

        if (attribute is not null)
        {
            identityType = attribute.GetType().GetGenericArguments()[0];
            return true;
        }

        identityType = null!;
        return false;
    }
}
