// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Discovers and builds projection definitions from model-bound attributes.
/// </summary>
/// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> to use for discovering client artifacts.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
/// <param name="logger"><see cref="ILogger{Projections}"/> for logging - the same category the fluent path logs its build failures to.</param>
/// <param name="currentEventStoreName">
/// The name of the event store the projections belong to.
/// Used to detect when event types belong to the same store, which means event-log rather than inbox routing.
/// </param>
internal class ModelBoundProjections(
    IClientArtifactsProvider clientArtifactsProvider,
    INamingPolicy namingPolicy,
    IEventTypes eventTypes,
    ILogger<Projections> logger,
    string? currentEventStoreName = null) : IModelBoundProjections
{
    /// <summary>
    /// Discovers all model-bound projections.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
    /// <remarks>
    /// Only types that are not used as children or sub-objects in other projections are considered
    /// as standalone projections. Types referenced via <see cref="ChildrenFromAttribute{TEvent}"/>
    /// are excluded from being treated as independent projections, as they are part of parent
    /// projection definitions.
    /// <para>
    /// A read model that cannot be built costs itself and nothing else. Building the whole set in one expression
    /// meant the first failure abandoned every definition - including the fluent ones, whose assignment in
    /// <see cref="Projections.Discover"/> comes after this call - and left a client whose read side was silently
    /// never wired while the write side kept working. The fluent path already isolates and logs per projection;
    /// this is the same treatment for the model-bound one.
    /// </para>
    /// </remarks>
    public IDictionary<Type, ProjectionDefinition> Discover()
    {
        var allCandidateTypes = clientArtifactsProvider.ModelBoundProjections.ToList();
        var typesUsedAsChildrenOrSubObjects = CollectTypesUsedAsChildrenOrSubObjects(allCandidateTypes);
        var rootProjectionTypes = allCandidateTypes.Except(typesUsedAsChildrenOrSubObjects).ToList();

        var builder = new ModelBoundProjectionBuilder(namingPolicy, eventTypes, currentEventStoreName);
        var definitions = new Dictionary<Type, ProjectionDefinition>();

        foreach (var rootProjectionType in rootProjectionTypes)
        {
            try
            {
                definitions.Add(rootProjectionType, builder.Build(rootProjectionType));
            }
#pragma warning disable CA1031 // One unbuildable read model must not be able to take the rest of the read side with it.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.FailedToCreateModelBoundProjectionDefinition(rootProjectionType, ex);
            }
        }

        return definitions;
    }

    static HashSet<Type> CollectTypesUsedAsChildrenOrSubObjects(IEnumerable<Type> candidateTypes)
    {
        var referencedTypes = new HashSet<Type>();

        foreach (var type in candidateTypes)
        {
            var referencedByType = new HashSet<Type>();
            CollectChildTypesFromType(type, referencedByType);
            referencedByType.Remove(type);
            foreach (var referenced in referencedByType)
            {
                referencedTypes.Add(referenced);
            }
        }

        return referencedTypes;
    }

    static void CollectChildTypesFromType(Type type, HashSet<Type> referencedTypes)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        if (primaryConstructor is not null)
        {
            foreach (var parameter in primaryConstructor.GetParameters())
            {
                CollectChildTypesFromParameter(parameter, referencedTypes);
            }
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (primaryConstructor?.GetParameters().Any(p => p.Name?.Equals(property.Name, StringComparison.OrdinalIgnoreCase) == true) == true)
            {
                continue;
            }

            CollectChildTypesFromProperty(property, referencedTypes);
        }
    }

    static void CollectChildTypesFromParameter(ParameterInfo parameter, HashSet<Type> referencedTypes)
    {
        var hasChildrenFromAttribute = parameter.GetCustomAttributes()
            .Any(attr => attr.GetType().IsGenericType &&
                        attr.GetType().GetGenericTypeDefinition() == typeof(ChildrenFromAttribute<>));

        if (hasChildrenFromAttribute)
        {
            var childType = GetChildType(parameter.ParameterType);
            if (childType is not null && referencedTypes.Add(childType))
            {
                CollectChildTypesFromType(childType, referencedTypes);
            }
        }
        else if (IsComplexType(parameter.ParameterType) && referencedTypes.Add(parameter.ParameterType))
        {
            CollectChildTypesFromType(parameter.ParameterType, referencedTypes);
        }
    }

    static void CollectChildTypesFromProperty(PropertyInfo property, HashSet<Type> referencedTypes)
    {
        var hasChildrenFromAttribute = property.GetCustomAttributes()
            .Any(attr => attr.GetType().IsGenericType &&
                        attr.GetType().GetGenericTypeDefinition() == typeof(ChildrenFromAttribute<>));

        if (hasChildrenFromAttribute)
        {
            var childType = GetChildType(property.PropertyType);
            if (childType is not null && referencedTypes.Add(childType))
            {
                CollectChildTypesFromType(childType, referencedTypes);
            }
        }
        else if (IsComplexType(property.PropertyType) && referencedTypes.Add(property.PropertyType))
        {
            CollectChildTypesFromType(property.PropertyType, referencedTypes);
        }
    }

    static Type? GetChildType(Type propertyType)
    {
        if (propertyType.IsGenericType)
        {
            var genericDef = propertyType.GetGenericTypeDefinition();
            if (genericDef == typeof(IEnumerable<>) || genericDef.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                return propertyType.GetGenericArguments()[0];
            }
        }

        var enumerableInterface = propertyType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0];
    }

    static bool IsComplexType(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid) || type.IsEnum)
        {
            return false;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return false;
        }

        return type.IsClass || type.IsValueType;
    }
}
