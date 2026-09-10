// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// An <see cref="IInstancesOf{T}"/> that yields only the implementations from Arc's and Chronicle's own
/// assemblies - the in-process kernel pipeline's counterpart of what a real kernel host's discovery would
/// find, since a kernel host has no application assemblies loaded. See <see cref="InProcessCommandPipeline"/>
/// for why the host application's implementations must stay out of that pipeline.
/// </summary>
/// <typeparam name="T">The extension point to yield kernel-side implementations of.</typeparam>
/// <param name="types">The type universe Arc's discovery produced for the pipeline.</param>
/// <param name="provider">The pipeline's service provider implementations are resolved from.</param>
/// <remarks>
/// Each implementation is resolved from the pipeline's service provider rather than reflected into existence,
/// so Arc's built-ins with constructor dependencies construct exactly as they would in a real kernel host.
/// </remarks>
internal class KernelInstancesOf<T>(ITypes types, IServiceProvider provider) : IInstancesOf<T>
    where T : class
{
    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() =>
        types.FindMultiple<T>()
            .Where(IsKernelSide)
            .Select(type => (T)ActivatorUtilities.CreateInstance(provider, type))
            .GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    static bool IsKernelSide(Type type)
    {
        var assemblyName = type.Assembly.GetName().Name ?? string.Empty;
        return assemblyName.StartsWith("Cratis.Arc", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("Cratis.Chronicle", StringComparison.OrdinalIgnoreCase);
    }
}
