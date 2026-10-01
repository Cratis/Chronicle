// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// An <see cref="IInstancesOf{T}"/> that yields only the implementations a stand-alone kernel host would discover -
/// those from Arc and from the kernel itself - for a command extension point.
/// </summary>
/// <typeparam name="T">The extension point.</typeparam>
/// <param name="types">The type universe discovery produced for the host.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> implementations are resolved from.</param>
/// <param name="onlyForKernelCommands">
/// Whether to narrow only while the kernel executes one of its own commands (see <see cref="KernelCommandExecution"/>),
/// yielding every discovered implementation otherwise. Used where the kernel shares its host with an application
/// whose own commands must keep their extension points.
/// </param>
/// <remarks>
/// <para>
/// Discovery in an in-process host sweeps up the application's assemblies as well as the kernel's. Without narrowing,
/// the kernel's internal commands run through the application's command filters, execution scopes, context values
/// providers, response value handlers and authorization opinions - so an application that denies by default refuses the
/// kernel's own appends, and a transactional scope that resolves the application's event store deadlocks a host whose
/// event store is still waiting on that very kernel command.
/// </para>
/// <para>
/// Kernel-side means the Arc assemblies the kernel is built on and the kernel's own assembly. A kernel host never
/// loads the Chronicle client's Arc integration, so its extension points are not kernel-side either.
/// </para>
/// </remarks>
internal sealed class KernelSideInstancesOf<T>(ITypes types, IServiceProvider serviceProvider, bool onlyForKernelCommands) : IInstancesOf<T>
    where T : class
{
    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        var narrow = !onlyForKernelCommands || KernelCommandExecution.IsActive;
        foreach (var type in types.FindMultiple<T>().Where(type => !narrow || KernelCommandExtensionPoints.IsKernelSide(type)))
        {
            yield return (T)(serviceProvider.GetService(type) ?? ActivatorUtilities.CreateInstance(serviceProvider, type));
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
