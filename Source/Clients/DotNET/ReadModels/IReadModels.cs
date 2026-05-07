// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a system that works with read models in the system.
/// </summary>
public interface IReadModels
{
    /// <summary>
    /// Register the read models in the system.
    /// </summary>
    /// <returns>An awaitable task.</returns>
    Task Register();

    /// <summary>
    /// Register a specific read model type.
    /// </summary>
    /// <typeparam name="TReadModel">The type of the read model to register.</typeparam>
    /// <returns>An awaitable task.</returns>
    Task Register<TReadModel>();

    /// <summary>
    /// Get a read model instance by its key.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="key">The <see cref="ReadModelKey"/> to get instance for.</param>
    /// <param name="sessionId">Optional <see cref="ReadModelSessionId"/> to get for a specific session.</param>
    /// <returns>The read model instance.</returns>
    Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key, ReadModelSessionId? sessionId = null);

    /// <summary>
    /// Get a read model instance by its key.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="key">The <see cref="ReadModelKey"/> to get instance for.</param>
    /// <param name="sessionId">Optional <see cref="ReadModelSessionId"/> to get for a specific session.</param>
    /// <returns>The read model instance.</returns>
    Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null);

    /// <summary>
    /// Get all instances of a read model by applying all events.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="eventCount">Optional maximum number of events to process. Defaults to <see cref="EventCount.Unlimited"/>.</param>
    /// <returns>Collection of read model instances.</returns>
    Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(EventCount? eventCount = null);

    /// <summary>
    /// Get snapshots of a read model grouped by CorrelationId by walking through events from the beginning.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model.</typeparam>
    /// <param name="readModelKey"><see cref="ReadModelKey"/> to get snapshots for.</param>
    /// <returns>Collection of <see cref="ReadModelSnapshot{TReadModel}"/>.</returns>
    Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey);

    /// <summary>
    /// Observe changes for a specific read model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to observe changes for.</typeparam>
    /// <returns>An observable of <see cref="ReadModelChangeset{TReadModel}"/>.</returns>
    IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>();

    /// <summary>
    /// Dehydrate a session.
    /// </summary>
    /// <param name="sessionId">The <see cref="ReadModelSessionId"/> to dehydrate.</param>
    /// <param name="readModelType">Type of read model.</param>
    /// <param name="readModelKey"><see cref="ReadModelKey"/> to dehydrate for.</param>
    /// <returns>Awaitable task.</returns>
    Task DehydrateSession(ReadModelSessionId sessionId, Type readModelType, ReadModelKey readModelKey);

    /// <summary>
    /// Release (decrypt) PII-annotated properties in a read model instance by deriving the subject from the instance itself.
    /// </summary>
    /// <remarks>
    /// The subject is resolved by looking for a property or constructor parameter decorated with <see cref="SubjectAttribute"/>,
    /// falling back to a property named <c>Id</c>. If no subject can be derived, or the read model has no compliance-annotated
    /// properties, the original instance is returned unchanged.
    /// If decryption fails (e.g. the encryption key has been permanently deleted), the original instance is returned and
    /// the failure is logged.
    /// </remarks>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="instance">The read model instance to decrypt.</param>
    /// <returns>The decrypted instance, or the original when release is not applicable or fails.</returns>
    Task<TReadModel> Release<TReadModel>(TReadModel instance);

    /// <summary>
    /// Release (decrypt) PII-annotated properties in a collection of read model instances.
    /// </summary>
    /// <remarks>
    /// The subject is derived from each instance individually. See <see cref="Release{TReadModel}(TReadModel)"/> for details.
    /// </remarks>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="instances">The collection of instances to decrypt.</param>
    /// <returns>The decrypted instances.</returns>
    Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances);

    /// <summary>
    /// Release (decrypt) PII-annotated properties in a read model instance using an explicit subject.
    /// </summary>
    /// <remarks>
    /// If decryption fails (e.g. the encryption key has been permanently deleted), the original instance is returned and
    /// the failure is logged.
    /// </remarks>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="subject">The explicit <see cref="Subject"/> used as the encryption key identifier.</param>
    /// <param name="instance">The read model instance to decrypt.</param>
    /// <returns>The decrypted instance, or the original when release fails.</returns>
    Task<TReadModel> Release<TReadModel>(Subject subject, TReadModel instance);
}
