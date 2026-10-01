// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Keeps the kernel's own command executions to the command extension points a stand-alone kernel host would discover,
/// when the kernel runs in-process inside a host that also discovers an application's types.
/// </summary>
/// <remarks>
/// The narrowed extension points are the ones the Arc command pipeline applies to every command it executes. See
/// <see cref="KernelSideInstancesOf{T}"/> for why they must not reach the kernel's commands.
/// </remarks>
internal static class KernelCommandExtensionPoints
{
    static readonly Assembly[] _kernelSideAssemblies =
    [
        typeof(ICommandPipeline).Assembly,
        typeof(AspNetAuthorizationAttributeEvaluator).Assembly,
        typeof(KernelCommandExtensionPoints).Assembly
    ];

    /// <summary>
    /// Narrows the command extension points to kernel-side implementations for every command, for a command pipeline
    /// that only ever executes the kernel's commands.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> of the kernel's own command pipeline.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddKernelSideCommandExtensionPoints(this IServiceCollection services) =>
        services.AddNarrowed(onlyForKernelCommands: false);

    /// <summary>
    /// Narrows the command extension points to kernel-side implementations while the kernel executes one of its own
    /// commands, for a host where the kernel shares the command pipeline with an application. The application's
    /// commands keep every discovered implementation.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> of the shared host.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddKernelSideCommandExtensionPointsForKernelCommands(this IServiceCollection services) =>
        services.AddNarrowed(onlyForKernelCommands: true);

    /// <summary>
    /// Checks whether a type comes from an assembly a stand-alone kernel host discovers: the Arc assemblies the kernel is
    /// built on, or the kernel's own.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it is kernel-side, false if not.</returns>
    internal static bool IsKernelSide(Type type) => _kernelSideAssemblies.Contains(type.Assembly);

    static IServiceCollection AddNarrowed(this IServiceCollection services, bool onlyForKernelCommands) =>
        services
            .AddNarrowed<ICommandFilter>(onlyForKernelCommands)
            .AddNarrowed<ICommandExecutionScope>(onlyForKernelCommands)
            .AddNarrowed<ICommandContextValuesProvider>(onlyForKernelCommands)
            .AddNarrowed<ICommandResponseValueHandler>(onlyForKernelCommands)
            .AddNarrowed<IAuthorizationAttributeEvaluator>(onlyForKernelCommands)
            .AddNarrowed<IAnonymousEvaluator>(onlyForKernelCommands)
            .AddNarrowed<IFallbackAuthorizationEvaluator>(onlyForKernelCommands);

    /// <summary>
    /// Registers the narrowed <see cref="IInstancesOf{T}"/> as transient, as the open-generic registration is, so each
    /// resolution activates implementations from the provider it was resolved from - the command's own scope when Arc
    /// resolves it there.
    /// </summary>
    /// <typeparam name="T">The extension point.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register in.</param>
    /// <param name="onlyForKernelCommands">Whether to narrow only while the kernel executes one of its own commands.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    static IServiceCollection AddNarrowed<T>(this IServiceCollection services, bool onlyForKernelCommands)
        where T : class =>
        services.AddTransient<IInstancesOf<T>>(sp => new KernelSideInstancesOf<T>(sp.GetRequiredService<ITypes>(), sp, onlyForKernelCommands));
}
