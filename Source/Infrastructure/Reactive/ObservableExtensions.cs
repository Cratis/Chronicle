// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Chronicle.Reactive;

/// <summary>
/// Holds extension methods for <see cref="ISubject{T}"/>.
/// </summary>
public static class ObservableExtensions
{
    /// <summary>
    /// Completes the subject when the <see cref="CancellationToken"/> is cancelled.
    /// </summary>
    /// <typeparam name="TResult">Type of result.</typeparam>
    /// <param name="subject">The subject to complete.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The subject.</returns>
    public static ISubject<TResult> CompletedBy<TResult>(this ISubject<TResult> subject, CancellationToken cancellationToken)
    {
        cancellationToken.Register(subject.OnCompleted);
        return subject;
    }

    /// <summary>
    /// Invokes an action and wraps the observable result in a subject providing, with a cancellation token passed to the action that cancels when the subject is completed.
    /// </summary>
    /// <param name="instance">The instance to extend.</param>
    /// <param name="action">The action to invoke that returns an observable.</param>
    /// <typeparam name="TResult">Type of result.</typeparam>
    /// <returns>The subject.</returns>
#pragma warning disable RCS1175 // Unused 'this' parameter
#pragma warning disable IDE0060 // Remove unused parameter
    public static ISubject<TResult> InvokeAndWrapWithSubject<TResult>(this object instance, Func<CancellationToken, IObservable<TResult>> action)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1175 // Unused 'this' parameter
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var cts = new CancellationTokenSource();
#pragma warning restore CA2000 // Dispose objects before losing scope
        var subject = new Subject<TResult>();
        var observable = action(cts.Token);
        observable.Subscribe(subject);
        subject.Subscribe(_ => { }, _ => { }, () =>
        {
            cts.Cancel();
            cts.Dispose();
        });
        return subject;
    }
}
