// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Conventions;

/// <summary>
/// Represents a method that is represented as a convention based on a signature defined by a delegate type.
/// </summary>
public class ConventionSignature
{
    readonly IEnumerable<int> _genericParametersMatch;
    readonly ParameterInfo[] _parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConventionSignature"/> class.
    /// </summary>
    /// <param name="signature">The signature for the method. Must be a delegate type.</param>
    public ConventionSignature(Type signature)
    {
        SignatureNotDelegate.ThrowIfInvalid(signature);

        var genericArguments = signature.GetGenericArguments();
        var invokeMethod = signature.GetMethod("Invoke")!;
        _parameters = invokeMethod.GetParameters();

        _genericParametersMatch = genericArguments.Select((arg, index) => new
        {
            _parameters.Select((parameter, index) => new
            {
                ParameterIndex = index!,
                parameter.ParameterType
            }).FirstOrDefault(_ => _.ParameterType == arg)?.ParameterIndex
        }).Where(_ => _.ParameterIndex.HasValue).Select(_ => _.ParameterIndex!.Value).ToArray();
        Signature = signature;
    }

    /// <summary>
    /// Gets the signature.
    /// </summary>
    public Type Signature { get; }

    /// <summary>
    /// Check if a method matches the signature of the convention method.
    /// </summary>
    /// <param name="method"><see cref="MethodInfo"/> to check.</param>
    /// <returns>True if it matches, false if not.</returns>
    public bool Matches(MethodInfo method)
    {
        if (method.Name == "Equals") return false;
        var methodParameters = method.GetParameters();
        if (methodParameters.Length != _parameters.Length)
        {
            return false;
        }

        MethodInfo invokeMethod;
        if (_genericParametersMatch.Any())
        {
            var genericParameters = _genericParametersMatch.Select(_ => methodParameters[_].ParameterType).ToArray();
            var genericTypeDef = Signature.GetGenericTypeDefinition().MakeGenericType(genericParameters);
            invokeMethod = genericTypeDef.GetMethod("Invoke")!;
        }
        else
        {
            invokeMethod = Signature.GetMethod("Invoke")!;
        }
        var actualParameters = invokeMethod.GetParameters();

        if (method.ReturnType != invokeMethod.ReturnType)
        {
            return false;
        }

        return methodParameters.Select(_ => _.ParameterType).SequenceEqual(actualParameters.Select(_ => _.ParameterType));
    }
}
