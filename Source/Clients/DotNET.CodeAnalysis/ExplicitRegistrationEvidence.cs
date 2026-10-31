// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Immutable, compilation-local evidence of possible explicit registrations, not proof of runtime membership.
/// </summary>
internal sealed class ExplicitRegistrationEvidence
{
    static readonly ConditionalWeakTable<Compilation, Lazy<ExplicitRegistrationEvidence>> _cache = new();

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
    /// Get the shared evidence for a compilation.
    /// </summary>
    /// <param name="compilation">Compilation to inspect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The shared explicit registrations.</returns>
    internal static ExplicitRegistrationEvidence For(Compilation compilation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var evidence = _cache.GetValue(compilation, key => new Lazy<ExplicitRegistrationEvidence>(() => Create(key, cancellationToken)));
        try
        {
            return evidence.Value;
        }
        catch (OperationCanceledException)
        {
            _cache.Remove(compilation);
            throw;
        }
    }

    /// <summary>
    /// Checks an event attribute before requesting shared registration evidence.
    /// </summary>
    /// <param name="type">The candidate event type.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    /// <returns>Whether the type is an attributed or explicitly registered event.</returns>
    internal static bool IsKnownEventType(ITypeSymbol type, Compilation compilation, CancellationToken cancellationToken) =>
        WellKnownTypes.HasEventTypeAttribute(type) || For(compilation, cancellationToken).IsEventType(type, compilation);

    /// <summary>
    /// Collects closed generic and typeof nominations through the actual ExplicitArtifacts method symbols.
    /// </summary>
    /// <param name="compilation">The owning compilation.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    /// <returns>An immutable set of possible registrations.</returns>
    internal static ExplicitRegistrationEvidence Create(Compilation compilation, CancellationToken cancellationToken) =>
        Create(compilation, tree => compilation.GetSemanticModel(tree), cancellationToken);

    /// <summary>
    /// Build evidence using the supplied semantic model factory.
    /// </summary>
    /// <param name="compilation">Compilation to inspect.</param>
    /// <param name="getSemanticModel">Factory for syntax trees that contain candidate registrations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The explicit registrations found in the compilation.</returns>
    internal static ExplicitRegistrationEvidence Create(Compilation compilation, Func<SyntaxTree, SemanticModel> getSemanticModel, CancellationToken cancellationToken)
    {
        var events = ImmutableHashSet.CreateBuilder<ITypeSymbol>(SymbolEqualityComparer.Default);
        var reactors = ImmutableHashSet.CreateBuilder<ITypeSymbol>(SymbolEqualityComparer.Default);
        var reducers = ImmutableHashSet.CreateBuilder<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var operation in RegistrationCalls(compilation, getSemanticModel, cancellationToken))
        {
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

    static IEnumerable<IInvocationOperation> RegistrationCalls(Compilation compilation, Func<SyntaxTree, SemanticModel> getSemanticModel, CancellationToken cancellationToken)
    {
        var artifacts = compilation.GetTypeByMetadataName("Cratis.Chronicle.Registrations.ExplicitArtifacts");
        if (artifacts is null) yield break;
        foreach (var tree in compilation.SyntaxTrees)
        {
            SemanticModel? model = null;
            foreach (var invocation in tree.GetRoot(cancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsRegistration(invocation)) continue;
                model ??= getSemanticModel(tree);
                var operation = RegistrationCall(model, invocation, artifacts, cancellationToken);
                if (operation is not null) yield return operation;
            }
        }
    }

    static IInvocationOperation? RegistrationCall(SemanticModel model, InvocationExpressionSyntax invocation, INamedTypeSymbol artifacts, CancellationToken cancellationToken)
    {
        var operation = model.GetOperation(invocation, cancellationToken) as IInvocationOperation;
        return operation is not null && SymbolEqualityComparer.Default.Equals(operation.TargetMethod.ContainingType, artifacts) ? operation : null;
    }

    static bool IsRegistration(InvocationExpressionSyntax invocation)
    {
        var name = InvocationName.Of(invocation);
        return name == "RegisterEventType" || name == "RegisterReactor" || name == "RegisterReducer";
    }

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
