// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Aksio.Cratis.Compliance
{
    /// <summary>
    /// Defines a system that can handle a property for a specific <see cref="ComplianceMetadataType"/>.
    /// </summary>
    public interface IJsonCompliancePropertyValueHandler
    {
        /// <summary>
        /// Gets the <see cref="ComplianceMetadataType"/> it supports.
        /// </summary>
        ComplianceMetadataType Type { get; }

        /// <summary>
        /// Apply to the given value.
        /// </summary>
        /// <param name="identifier">Identifier to use.</param>
        /// <param name="value">Value to apply to.</param>
        /// <returns>Applied value.</returns>
        Task<JToken> Apply(string identifier, JToken value);

        /// <summary>
        /// Release a given value.
        /// </summary>
        /// <param name="identifier">Identifier to use.</param>
        /// <param name="value">Value to release.</param>
        /// <returns>Released value.</returns>
        Task<JToken> Release(string identifier, JToken value);
    }
}
