// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Immutable, compilation-local evidence of possible explicit registrations, not proof of runtime membership.
/// </summary>
internal sealed class ExplicitRegistrationEvidence
{
    readonly ImmutableHashSet<ITypeSymbol> _events;
    readonly ImmutableHashSet<ITypeSymbol> _reactors;
    readonly ImmutableHashSet<ITypeSymbol> _reducers;

    ExplicitRegistrationEvidence(ImmutableHashSet<ITypeSymbol> events, ImmutableHashSet<ITypeSymbol> reactors, ImmutableHashSet<ITypeSymbol> reducers)
    {
        _events = events;
        _reactors = reactors;
        _reducers = reducers;
    }

    /// <summary>
    /// Gets the explicitly nominated event types.
    /// </summary>
    internal IEnumerable<ITypeSymbol> EventTypes => _events;

    /// <summary>
    /// Collects closed generic and typeof nominations through the actual ExplicitArtifacts method symbols.
    /// </summary>
    /// <param name="compilation">The owning compilation.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    /// <returns>An immutable set of possible registrations.</returns>
    internal static ExplicitRegistrationEvidence Create(Compilation compilation, CancellationToken cancellationToken)
    {
        var events = ImmutableHashSet.CreateBuilder<ITypeSymbol>(SymbolEqualityComparer.Default);
        var reactors = ImmutableHashSet.CreateBuilder<ITypeSymbol>(SymbolEqualityComparer.Default);
        var reducers = ImmutableHashSet.CreateBuilder<ITypeSymbol>(SymbolEqualityComparer.Default);
        var artifacts = compilation.GetTypeByMetadataName("Cratis.Chronicle.Registrations.ExplicitArtifacts");
        if (artifacts is not null)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var invocation in tree.GetRoot(cancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (model.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation ||
                        !SymbolEqualityComparer.Default.Equals(operation.TargetMethod.ContainingType, artifacts))
                    {
                        continue;
                    }

                    var target = operation.TargetMethod.Name switch
                    {
                        "RegisterEventType" => events,
                        "RegisterReactor" => reactors,
                        "RegisterReducer" => reducers,
                        _ => null
                    };
                    if (target is null) continue;
                    var type = RegisteredType(operation);
                    if (type is not null && IsClosed(type)) target.Add(type);
                }
            }
        }

        return new(events.ToImmutable(), reactors.ToImmutable(), reducers.ToImmutable());
    }

    /// <summary>
    /// Checks explicit reactor ownership.
    /// </summary>
    /// <param name="type">The observer type.</param>
    /// <returns>Whether the compilation nominates it.</returns>
    internal bool IsReactor(ITypeSymbol type) => _reactors.Contains(type);

    /// <summary>
    /// Checks explicit reducer ownership.
    /// </summary>
    /// <param name="type">The observer type.</param>
    /// <returns>Whether the compilation nominates it.</returns>
    internal bool IsReducer(ITypeSymbol type) => _reducers.Contains(type);

    /// <summary>
    /// Checks marker or explicit membership, including polymorphic dispatch from nominated events.
    /// </summary>
    /// <param name="type">The event parameter or return type.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>Whether event membership is possible.</returns>
    internal bool IsEventType(ITypeSymbol type, Compilation compilation) =>
        WellKnownTypes.HasEventTypeAttribute(type) || _events.Contains(type) ||
        (type is { SpecialType: not SpecialType.System_Object, IsSealed: false, TypeKind: TypeKind.Class or TypeKind.Interface } &&
         _events.Any(@event => compilation.ClassifyCommonConversion(@event, type) is { IsImplicit: true, IsUserDefined: false }));

    static ITypeSymbol? RegisteredType(IInvocationOperation invocation)
    {
        if (invocation.TargetMethod is { IsGenericMethod: true, TypeArguments.Length: 1 } method)
        {
            return method.TypeArguments[0];
        }

        var value = invocation.Arguments.FirstOrDefault(_ => _.Parameter?.Ordinal == 0)?.Value;
        while (value is IConversionOperation conversion) value = conversion.Operand;
        return value is ITypeOfOperation typeOf ? typeOf.TypeOperand : null;
    }

    static bool IsClosed(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => false,
        INamedTypeSymbol named => !named.IsUnboundGenericType && named.TypeArguments.All(IsClosed) && (named.ContainingType is null || IsClosed(named.ContainingType)),
        IArrayTypeSymbol array => IsClosed(array.ElementType),
        _ => true
    };
}
