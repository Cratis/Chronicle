// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Chronicle;

/// <summary>
/// Extension methods for working with <see cref="ISubject{T}"/>.
/// </summary>
internal static class SubjectExtensions
{
    /// <summary>
    /// Creates a derived <see cref="ISubject{T}"/> that applies a transform to every emission.
    /// The returned subject emits the transformed value immediately (using the last emitted source value)
    /// and continues emitting whenever the source publishes a new value.
    /// </summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <typeparam name="TResult">The result element type.</typeparam>
    /// <param name="source">The source subject.</param>
    /// <param name="transform">The transform function applied to each emission.</param>
    /// <returns>A new subject that emits transformed values.</returns>
    internal static ISubject<TResult> TransformSubject<TSource, TResult>(
        this ISubject<TSource> source,
        Func<TSource, TResult> transform)
    {
        var subject = new ReplaySubject<TResult>(1);
        source.Subscribe(
            value => subject.OnNext(transform(value)),
            subject.OnError,
            subject.OnCompleted);
        return subject;
    }
}
