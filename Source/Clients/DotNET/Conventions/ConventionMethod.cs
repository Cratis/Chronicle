// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Conventions;

/// <summary>
/// Represents a method that is represented as a convention based on a signature defined by a delegate type.
/// </summary>
public class ConventionMethod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConventionMethod"/> class.
    /// </summary>
    /// <param name="signature">The signature for the method. Must be a delegate type.</param>
    public ConventionMethod(Type signature)
    {
    }

    /// <summary>
    /// Check if a method matches the signature of the convention method.
    /// </summary>
    /// <param name="method"><see cref="MethodInfo"/> to check.</param>
    /// <returns>True if it matches, false if not.</returns>
    public bool Matches(MethodInfo method) => throw new NotImplementedException();

    /// <summary>
    /// Invoke the method, matching parameters to the best matching signature.
    /// </summary>
    /// <param name="target">Target to invoke on.</param>
    /// <param name="parameters">Params of parameters to pass. Parameters are optional.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// If no matching signature is found, an exception will be thrown.
    /// If the method by the matching signature does not return anything, null will be returned.
    /// </remarks>
    public Task<object> Invoke(object target, params object[] parameters) => throw new NotImplementedException();
}
