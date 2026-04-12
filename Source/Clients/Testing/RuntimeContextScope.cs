// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Manages the Orleans <c>RuntimeContext</c> for in-process grain construction without a silo.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Grain"/> base class constructor accesses <c>RuntimeContext.Current!.ObservableLifecycle</c>,
/// which requires a valid <see cref="IGrainContext"/> to be set as the current execution context. This class
/// uses reflection to call the internal <c>RuntimeContext.SetExecutionContext</c> method, following the same
/// approach used by the OrleansTestKit project.
/// </para>
/// <para>
/// Usage: wrap grain construction in a <see langword="using"/> block to ensure the context is properly cleaned up:
/// <code>
/// using (RuntimeContextScope.SetExecutionContext(testGrainContext))
/// {
///     var grain = new MyGrain(dependencies...);
/// }
/// </code>
/// </para>
/// </remarks>
internal static class RuntimeContextScope
{
    static readonly Action<IGrainContext?> _setContext = CreateSetContextAction();

    /// <summary>
    /// Sets the given <see cref="IGrainContext"/> as the current Orleans runtime context and returns
    /// an <see cref="IDisposable"/> that clears the context on disposal.
    /// </summary>
    /// <param name="context">The <see cref="IGrainContext"/> to set as current.</param>
    /// <returns>An <see cref="IDisposable"/> that resets the runtime context when disposed.</returns>
    internal static IDisposable SetContext(IGrainContext context) => new Scope(context);

    static Action<IGrainContext?> CreateSetContextAction()
    {
        var assembly = typeof(GrainId).Assembly;
        var contextType = assembly.GetType("Orleans.Runtime.RuntimeContext")
            ?? throw new InvalidOperationException(
                "Could not find Orleans.Runtime.RuntimeContext in Orleans.Core.Abstractions. " +
                "This may indicate an incompatible Orleans version.");

        var method = contextType.GetMethod("SetExecutionContext", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Could not find RuntimeContext.SetExecutionContext method. " +
                "This may indicate an incompatible Orleans version.");

        // SetExecutionContext has an out parameter for the previous context — wrap it
        // in a simple Action since we don't need the previous value.
        return ctx =>
        {
            var args = new object?[] { ctx, null };
            method.Invoke(null, args);
        };
    }

    sealed class Scope : IDisposable
    {
        internal Scope(IGrainContext context)
        {
            _setContext(context);
        }

        public void Dispose()
        {
            _setContext(null);
        }
    }
}
