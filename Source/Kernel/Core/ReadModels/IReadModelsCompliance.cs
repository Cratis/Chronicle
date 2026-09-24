// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines apply/release operations for read model instances - covering both compliance (<c language="csharp">[PII]</c>) and
/// security (<c language="csharp">[Encrypted]</c>) schema metadata alike, via the same generalized dispatch. A read model may
/// carry either, both, or neither; every method here handles whichever the schema actually declares.
/// </summary>
public interface IReadModelsCompliance
{
    /// <summary>
    /// Encrypts compliance- and security-annotated properties in a read model instance and writes the resolved subject.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read model belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the read model belongs to.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> describing the read model's properties.</param>
    /// <param name="identifier">The subject identifier used as the encryption key reference.</param>
    /// <param name="instance">The <see cref="ExpandoObject"/> read model instance to encrypt.</param>
    /// <returns>A new <see cref="ExpandoObject"/> with the protected fields encrypted and the subject written.</returns>
    Task<ExpandoObject> Apply(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        string identifier,
        ExpandoObject instance);

    /// <summary>
    /// Release (decrypt) compliance- and security-annotated properties in a read model <see cref="JsonObject"/> using the stored subject.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read model belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the read model belongs to.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> describing the read model's properties.</param>
    /// <param name="instance">The <see cref="JsonObject"/> read model instance to decrypt.</param>
    /// <returns>The decrypted instance, or the original when release is not applicable.</returns>
    Task<JsonObject> ReleaseJson(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        JsonObject instance);

    /// <summary>
    /// Release (decrypt) compliance- and security-annotated properties in a single read model instance.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read model belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the read model belongs to.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> describing the read model's properties.</param>
    /// <param name="instance">The read model instance to decrypt.</param>
    /// <returns>The decrypted instance, or the original when release is not applicable.</returns>
    Task<ExpandoObject> Release(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        ExpandoObject instance);

    /// <summary>
    /// Release (decrypt) compliance- and security-annotated properties in a collection of read model instances.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read models belong to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the read models belong to.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> describing the read models' properties.</param>
    /// <param name="instances">The collection of instances to decrypt.</param>
    /// <returns>The decrypted instances.</returns>
    Task<IEnumerable<ExpandoObject>> Release(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        IEnumerable<ExpandoObject> instances);
}
