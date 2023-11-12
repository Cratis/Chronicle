// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;

namespace Aksio.Cratis.Conventions;

/// <summary>
/// Represents a type with <see cref="ConventionSignature">convention signatures</see>.
/// </summary>
public class TypeWithConventionSignatures
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeWithConventionSignatures"/> class.
    /// </summary>
    /// <param name="target">Target type.</param>
    /// <param name="signatures">Collection of <see cref="ConventionSignature"/>.</param>
    /// <param name="methodFilter">Optional filter callback.</param>
    public TypeWithConventionSignatures(Type target, IEnumerable<ConventionSignature> signatures, Func<MethodInfo, bool>? methodFilter = default)
    {
        var methods = target.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).AsEnumerable();
        if (methodFilter != null)
        {
            methods = methods
                .Where(_ => methodFilter(_))
                .ToArray();
        }

        var conventionMethods = new List<ConventionMethod>();

        foreach (var method in methods)
        {
            var signature = signatures.FirstOrDefault(_ => _.Matches(method));
            if (signature != null)
            {
                var convention = new ConventionMethod(method, signature);
                conventionMethods.Add(convention);
            }
        }

        Methods = conventionMethods.ToImmutableList();
    }

    /// <summary>
    /// Gets a collection of <see cref="ConventionMethod">convention methods</see>.
    /// </summary>
    public IImmutableList<ConventionMethod> Methods { get; }

    /// <summary>
    /// Check if a method can be invoked with the given parameters.
    /// </summary>
    /// <param name="parameters">Parameters to check with.</param>
    /// <returns>True if it can invoke, false if not.</returns>
    public bool CanInvoke(params object[] parameters) => Methods.Any(_ => _.CanInvoke(parameters));

    /// <summary>
    /// Invoke the method matching the given parameters.
    /// </summary>
    /// <param name="target">Target to invoke on.</param>
    /// <param name="parameters">Parameters to invoke with.</param>
    /// <returns>Result, if any. This can be null if the method does not return anything.</returns>
    /// <exception cref="UnableToInvokeMethod">Thrown if method can't be invoked.</exception>
    public async Task<object> Invoke(object target, params object[] parameters)
    {
        var method = Methods.FirstOrDefault(_ => _.CanInvoke(parameters)) ?? throw new UnableToInvokeMethod();
        return await method.Invoke(target, parameters);
    }
}
