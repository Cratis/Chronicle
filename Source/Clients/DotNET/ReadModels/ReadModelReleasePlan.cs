// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The per-property release declarations a read model type carries, resolved once per type.
/// </summary>
/// <remarks>
/// A read model without a single <see cref="ReleaseUnderAttribute"/> yields <see cref="Groups"/> empty and
/// <see cref="HasDeclarations"/> false, which is what keeps the undeclared release path exactly what it was:
/// one subject, one call, the whole payload.
/// </remarks>
/// <param name="Groups">The declared groups, one per property named as a subject source.</param>
internal sealed record ReadModelReleasePlan(IReadOnlyList<ReadModelReleaseGroup> Groups)
{
    static readonly ConcurrentDictionary<Type, ReadModelReleasePlan> _plans = new();
    static readonly ReadModelReleasePlan _none = new([]);

    /// <summary>
    /// Gets a value indicating whether the read model declares any per-property subject.
    /// </summary>
    public bool HasDeclarations => Groups.Count > 0;

    /// <summary>
    /// Resolve the plan for a read model type.
    /// </summary>
    /// <param name="readModelType">The read model <see cref="Type"/>.</param>
    /// <returns>The <see cref="ReadModelReleasePlan"/> for the type.</returns>
    /// <exception cref="ReleaseUnderPropertyNotFound">A declaration names a property the read model does not have.</exception>
    /// <exception cref="ReleaseUnderNotSupportedBelowReadModel">A declaration sits below the read model's own properties.</exception>
    public static ReadModelReleasePlan For(Type readModelType) => _plans.GetOrAdd(readModelType, Build);

    static ReadModelReleasePlan Build(Type readModelType)
    {
        var properties = readModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var declared = properties
            .Select(property => (Property: property, Declaration: FindDeclaration(property)))
            .Where(candidate => candidate.Declaration is not null)
            .ToArray();

        ReadModelReleaseDeclarationScanner.ThrowIfDeclaredBelowReadModel(readModelType, properties);

        if (declared.Length == 0)
        {
            return _none;
        }

        var groups = declared
            .GroupBy(candidate => candidate.Declaration!.PropertyName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ReadModelReleaseGroup(
                ResolveSubjectProperty(readModelType, properties, group.First().Property.Name, group.Key),
                [.. group.Select(candidate => candidate.Property)]))
            .ToArray();

        return new ReadModelReleasePlan(groups);
    }

    static ReleaseUnderAttribute? FindDeclaration(PropertyInfo property) =>
        property.GetCustomAttribute<ReleaseUnderAttribute>(inherit: false) ??
        FindDeclarationOnConstructorParameter(property);

    static ReleaseUnderAttribute? FindDeclarationOnConstructorParameter(PropertyInfo property)
    {
        // An attribute written without an explicit target on a positional record lands on the primary
        // constructor's parameter, not on the generated property — the same shape [PII] and [Subject] are
        // both read through, so [ReleaseUnder] has to be read the same way or it silently does nothing on
        // the most idiomatic way to declare a read model.
        if (property.DeclaringType is null)
        {
            return null;
        }

        var constructor = property.DeclaringType.GetConstructors().MaxBy(candidate => candidate.GetParameters().Length);
        return constructor?
            .GetParameters()
            .FirstOrDefault(parameter => string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase))?
            .GetCustomAttribute<ReleaseUnderAttribute>(inherit: false);
    }

    static PropertyInfo ResolveSubjectProperty(Type readModelType, PropertyInfo[] properties, string declaringPropertyName, string subjectPropertyName) =>
        properties.FirstOrDefault(property => string.Equals(property.Name, subjectPropertyName, StringComparison.OrdinalIgnoreCase)) ??
        throw new ReleaseUnderPropertyNotFound(readModelType, declaringPropertyName, subjectPropertyName);
}
