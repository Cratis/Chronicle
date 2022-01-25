// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Execution
{
    /// <summary>
    /// Represents the unique identifier of a tenant in the system.
    /// </summary>
    /// <param name="Value">Actual value.</param>
    public record TenantId(Guid Value) : ConceptAs<Guid>(Value)
    {
        /// <summary>
        /// The value used when a <see cref="TenantId"/> is not set.
        /// </summary>
        public static readonly TenantId NotSet = Guid.Empty;

        /// <summary>
        /// The development tenant used during development as a fallback.
        /// </summary>
        public static readonly TenantId Development = Guid.Parse("3352d47d-c154-4457-b3fb-8a2efb725113");

        /// <summary>
        /// Implicitly convert from <see cref="Guid"/> to <see cref="TenantId"/>.
        /// </summary>
        /// <param name="id"><see cref="Guid"/> to convert from.</param>
        /// <returns>A new <see cref="TenantId"/>.</returns>
        public static implicit operator TenantId(Guid id) => new(id);

        /// <summary>
        /// Implicitly convert from <see cref="string"/> representation of a <see cref="Guid"/> to <see cref="TenantId"/>.
        /// </summary>
        /// <param name="id"><see cref="string"/> to convert from.</param>
        /// <returns>A new <see cref="TenantId"/>.</returns>
        public static implicit operator TenantId(string id) => Guid.Parse(id);
    }
}
