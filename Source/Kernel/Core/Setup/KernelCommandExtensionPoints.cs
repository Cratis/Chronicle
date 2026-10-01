// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Keeps the kernel's own command executions to the command extension points a stand-alone kernel host would discover,
/// when the kernel runs in-process inside a host that also discovers an application's types.
/// </summary>
/// <remarks>
/// The narrowed extension points are the ones the Arc command pipeline applies to every command it executes. See
/// <see cref="KernelSideInstancesOf{T}"/> for why they must not reach the kernel's commands. The pipeline's other
/// discovered extension point, <c language="csharp">ICommandHandlerProvider</c>, is deliberately left alone: it only locates handlers and
/// is read once, so it cannot be told apart per command.
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

    /// <summary>
    /// Checks whether a discovered type is one of the extension point implementations, rather than part of the machinery
    /// that narrows them.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it is an implementation to offer, false if not.</returns>
    internal static bool IsImplementation(Type type) => type != typeof(KernelAwareKeyResolver);

    static IServiceCollection AddNarrowed(this IServiceCollection services, bool onlyForKernelCommands) =>
        services
            .AddNarrowed<ICommandFilter>(onlyForKernelCommands)
            .AddNarrowed<ICommandExecutionScope>(onlyForKernelCommands)
            .AddNarrowed<ICommandContextValuesProvider>(onlyForKernelCommands)
            .AddNarrowed<ICommandResponseValueHandler>(onlyForKernelCommands)
            .AddNarrowed<IAuthorizationAttributeEvaluator>(onlyForKernelCommands)
            .AddNarrowed<IAnonymousEvaluator>(onlyForKernelCommands)
            .AddNarrowed<IFallbackAuthorizationEvaluator>(onlyForKernelCommands)
            .AddNarrowed<IUnresolvableDependencyClassifier>(onlyForKernelCommands)
            .AddNarrowedKeyResolvers(onlyForKernelCommands);

    /// <summary>
    /// Narrows <see cref="ICanResolveKeyForCommand"/>. Arc keeps the rules it is given for the lifetime of the host, so
    /// where the kernel shares the host with an application a single rule choosing per call is registered instead
    /// (see <see cref="KernelAwareKeyResolver"/>).
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register in.</param>
    /// <param name="onlyForKernelCommands">Whether to narrow only while the kernel executes one of its own commands.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    static IServiceCollection AddNarrowedKeyResolvers(this IServiceCollection services, bool onlyForKernelCommands) =>
        onlyForKernelCommands
            ? services.AddTransient<IInstancesOf<ICanResolveKeyForCommand>>(sp => new KernelAwareKeyResolver(sp.GetRequiredService<ITypes>(), sp))
            : services.AddNarrowed<ICanResolveKeyForCommand>(false);

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

    /// <summary>
    /// The only <see cref="ICanResolveKeyForCommand"/> Arc is given in a host the kernel shares with an application,
    /// deciding per call which of the discovered rules apply: every one for the application's commands, the kernel-side
    /// ones while the kernel executes one of its own.
    /// </summary>
    /// <param name="types">The type universe discovery produced for the host.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> the rules are resolved from.</param>
    /// <remarks>
    /// <para>
    /// Arc's <c language="csharp">CommandKeys</c> reads the discovered rules once and keeps them for the lifetime of the
    /// host. Narrowing the collection per execution, as for the other command extension points, would therefore leave
    /// whichever command resolved a key first deciding the rules for every command after it. Handing Arc a single rule
    /// that makes the choice on each call keeps the decision per command, in the order Arc itself asks them: the rule Arc
    /// ships is asked last.
    /// </para>
    /// <para>
    /// It is itself the <see cref="IInstancesOf{T}"/> holding that single rule. It is a private type so type discovery
    /// does not offer it as a rule in any other host, and <see cref="IsImplementation"/> keeps it out of its own rules.
    /// </para>
    /// </remarks>
    sealed class KernelAwareKeyResolver(ITypes types, IServiceProvider serviceProvider) : ICanResolveKeyForCommand, IInstancesOf<ICanResolveKeyForCommand>
    {
        readonly KernelSideInstancesOf<ICanResolveKeyForCommand> _resolvers = new(types, serviceProvider, true);

        /// <inheritdoc/>
        public string? Resolve(object command)
        {
            var resolvers = _resolvers.ToArray();
            foreach (var resolver in resolvers.Where(_ => _ is not DefaultKeyForCommandResolver).Concat(resolvers.Where(_ => _ is DefaultKeyForCommandResolver)))
            {
                if (resolver.Resolve(command) is { Length: > 0 } key)
                {
                    return key;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public IEnumerator<ICanResolveKeyForCommand> GetEnumerator()
        {
            yield return this;
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
