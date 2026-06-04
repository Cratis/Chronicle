// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Defines operations for applying compliance release to read model instances.
/// </summary>
public interface IReadModelsCompliance
{
    /// <summary>
    /// Release (decrypt) PII-annotated properties in a single read model instance.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="schema">The read model schema.</param>
    /// <param name="instance">The read model instance to decrypt.</param>
    /// <returns>The decrypted instance, or the original when release is not applicable or fails.</returns>
    Task<ExpandoObject> Release(
        string eventStore,
        string @namespace,
        JsonSchema schema,
        ExpandoObject instance);

    /// <summary>
    /// Release (decrypt) PII-annotated properties in a collection of read model instances.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="schema">The read model schema.</param>
    /// <param name="instances">The collection of instances to decrypt.</param>
    /// <returns>The decrypted instances.</returns>
    Task<IList<ExpandoObject>> Release(
        string eventStore,
        string @namespace,
        JsonSchema schema,
        IEnumerable<ExpandoObject> instances);
}
