// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Extension methods for converting observables of <see cref="ReadModelChangeset{TReadModel}"/> to subjects.
/// </summary>
public static class ReadModelChangesetExtensions
{
    /// <summary>
    /// Convert an observable of <see cref="ReadModelChangeset{TReadModel}"/> to a subject of <typeparamref name="TReadModel"/> that emits the read model instances for non-removed changesets.
    /// The subject will emit the read model instance for each changeset where <see cref="ReadModelChangeset{TReadModel}.Removed"/> is false and <see cref="ReadModelChangeset{TReadModel}.ReadModel"/> is not null.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model.</typeparam>
    /// <param name="source">The source observable of <see cref="ReadModelChangeset{TReadModel}"/>.</param>
    /// <returns>A subject of <typeparamref name="TReadModel"/> that emits the read model instances for non-removed changesets.</returns>
    public static ISubject<TReadModel> ToObservableReadModel<TReadModel>(
        this IObservable<ReadModelChangeset<TReadModel>> source)
    {
        var subject = new Subject<TReadModel>();

        source.Subscribe(
            changeset =>
            {
                if (!changeset.Removed && changeset.ReadModel is not null)
                {
                    subject.OnNext(changeset.ReadModel);
                }
            },
            subject.OnError,
            subject.OnCompleted);

        return subject;
    }
}
