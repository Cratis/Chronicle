// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// The exact typed builder roles of one explicitly nominated plain migration.
/// </summary>
/// <param name="MigrationType">The original recipe CLR type.</param>
/// <param name="UpgradeType">The upgraded event type.</param>
/// <param name="PreviousType">The previous event type.</param>
/// <param name="Upcast">The synchronous typed upcast role.</param>
/// <param name="Downcast">The synchronous typed downcast role.</param>
/// <param name="MapValues">The optional synchronous value-map role.</param>
internal sealed record ExplicitMigrationMethods(Type MigrationType, Type UpgradeType, Type PreviousType, MethodInfo Upcast, MethodInfo Downcast, MethodInfo? MapValues)
{
    /// <summary>
    /// Validates the exact existing typed-builder roles without discovering artifacts by shape.
    /// </summary>
    /// <param name="type">The explicitly nominated plain class.</param>
    /// <returns>The selected typed methods and endpoint association.</returns>
    /// <exception cref="InvalidExplicitArtifact">The nominated class or any named role has an invalid or ambiguous shape.</exception>
    internal static ExplicitMigrationMethods For(Type type)
    {
        if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
        {
            throw Invalid(type, "A plain migration must be a concrete closed class.");
        }
        if (type.IsAssignableTo(typeof(IDisposable)) || type.IsAssignableTo(typeof(IAsyncDisposable)))
        {
            throw Invalid(type, "A plain migration cannot require disposal; migration providers do not own a disposal scope.");
        }

        var methods = PublicRoles(type).ToArray();
        var upcast = Select(type, methods, nameof(Upcast), optional: false)!;
        var upcastBuilder = ValidateRole(type, upcast, typeof(IEventMigrationBuilder<,>));
        var endpoints = upcastBuilder.GetGenericArguments();
        if (endpoints.Any(_ => _.ContainsGenericParameters))
        {
            throw Invalid(type, "Migration endpoints must be closed CLR types.");
        }

        var downcast = Select(type, methods, nameof(Downcast), optional: false)!;
        var downcastBuilder = ValidateRole(type, downcast, typeof(IEventMigrationBuilder<,>));
        if (downcastBuilder != typeof(IEventMigrationBuilder<,>).MakeGenericType(endpoints[1], endpoints[0]))
        {
            throw Invalid(type, "Downcast must reverse Upcast's exact endpoint types.");
        }

        var mapValues = Select(type, methods, nameof(MapValues), optional: true);
        if (mapValues is not null && ValidateRole(type, mapValues, typeof(IEventValueMapBuilder<,>)) !=
            typeof(IEventValueMapBuilder<,>).MakeGenericType(endpoints))
        {
            throw Invalid(type, "MapValues must use Upcast's exact endpoint types.");
        }

        return new(type, endpoints[0], endpoints[1], upcast, downcast, mapValues);
    }

    /// <summary>
    /// Activates the original recipe with explicit owning-registry injection and creates its typed-base adapter.
    /// </summary>
    /// <param name="serviceProvider">The provider for the recipe's ordinary constructor dependencies.</param>
    /// <param name="eventTypes">The owning store's finalized registry.</param>
    /// <returns>The adapter using the existing untyped migration entry points.</returns>
    internal IEventTypeMigration Create(IServiceProvider serviceProvider, IEventTypes eventTypes)
    {
        var requestsRegistry = MigrationType.GetConstructors().Any(_ => _.GetParameters().Any(p => p.ParameterType == typeof(IEventTypes)));
        var recipe = requestsRegistry
            ? ActivatorUtilities.CreateInstance(serviceProvider, MigrationType, eventTypes)
            : ActivatorUtilities.CreateInstance(serviceProvider, MigrationType);
        var factory = typeof(ExplicitMigrationMethods).GetMethod(nameof(CreateAdapter), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(MigrationType, UpgradeType, PreviousType)
            .CreateDelegate<Func<IEventTypes, object, ExplicitMigrationMethods, IEventTypeMigration>>();

        return factory(eventTypes, recipe, this);
    }

    static ExplicitEventTypeMigration<TMigration, TUpgrade, TPrevious> CreateAdapter<TMigration, TUpgrade, TPrevious>(IEventTypes eventTypes, object recipe, ExplicitMigrationMethods methods)
        where TMigration : class => new(eventTypes, (TMigration)recipe, methods);

    static InvalidExplicitArtifact Invalid(Type type, string reason) => new(type, "migration", reason);

    static IEnumerable<MethodInfo> PublicRoles(Type type)
    {
        var overrides = new HashSet<MethodInfo>();
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var method in current.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(_ => _.Name.Equals(nameof(Upcast), StringComparison.Ordinal) || _.Name.Equals(nameof(Downcast), StringComparison.Ordinal) || _.Name.Equals(nameof(MapValues), StringComparison.Ordinal)))
            {
                // A virtual override represents one role; hidden and overloaded methods remain separate peers.
                if (overrides.Add(method.GetBaseDefinition()))
                {
                    yield return method;
                }
            }
        }
    }

    static MethodInfo? Select(Type type, MethodInfo[] methods, string name, bool optional)
    {
        var matching = methods.Where(_ => _.Name == name).ToArray();
        if (matching.Length == 0 && optional) return null;
        if (matching.Length != 1)
        {
            throw Invalid(type, $"Exactly one public instance '{name}' role is required{(optional ? " when supplied" : string.Empty)}.");
        }

        return matching[0];
    }

    static Type ValidateRole(Type type, MethodInfo method, Type builderDefinition)
    {
        var parameters = method.GetParameters();
        if (method.IsStatic || method.IsGenericMethod || method.ReturnType != typeof(void) ||
            method.IsDefined(typeof(AsyncStateMachineAttribute)) || parameters.Length != 1)
        {
            throw Invalid(type, $"'{method.Name}' must be a synchronous, non-generic public instance void method with one typed builder parameter.");
        }

        var parameter = parameters[0];
        var builderType = parameter.ParameterType;
        if (parameter.IsOptional || parameter.IsOut || parameter.IsDefined(typeof(ParamArrayAttribute)) || builderType.IsByRef ||
            !builderType.IsGenericType || builderType.GetGenericTypeDefinition() != builderDefinition)
        {
            throw Invalid(type, $"'{method.Name}' requires exactly '{builderDefinition.Name}' with an ordinary non-optional, non-params, non-byref parameter.");
        }

        return builderType;
    }
}
