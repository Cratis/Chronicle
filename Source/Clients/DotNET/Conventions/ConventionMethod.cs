// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Conventions;

/// <summary>
/// Represents a method that is represented as a convention based on a signature defined by a delegate type.
/// </summary>
public class ConventionMethod
{
    readonly ConventionSignature _signature;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConventionMethod"/> class.
    /// </summary>
    /// <param name="method">The underlying method.</param>
    /// <param name="signature">The signature it matches.</param>
    public ConventionMethod(MethodInfo method, ConventionSignature signature)
    {
        Method = method;
        _signature = signature;
    }

    /// <summary>
    /// Gets the method.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Check if a method can be invoked with the given parameters.
    /// </summary>
    /// <param name="parameters">Parameters to check with.</param>
    /// <returns>True if it can invoke, false if not.</returns>
    public bool CanInvoke(params object[] parameters)
    {
        var parameterTypes = parameters.Select(_ => _.GetType()).ToArray();
        var methodParameterTypes = Method.GetParameters().Select(_ => _.ParameterType).ToArray();
        var invokeMethod = _signature.Signature.GetMethod("Invoke")!;
        if (methodParameterTypes.Length != parameterTypes.Length)
        {
            return false;
        }

        if (Method.ReturnType != invokeMethod.ReturnType)
        {
            return false;
        }

        return methodParameterTypes.SequenceEqual(parameterTypes);
    }

    /// <summary>
    /// Invoke the method matching the given parameters.
    /// </summary>
    /// <param name="target">Target to invoke on.</param>
    /// <param name="parameters">Parameters to invoke with.</param>
    /// <returns>Result, if any. This can be null if the method does not return anything.</returns>
    public async Task<object> Invoke(object target, params object[] parameters)
    {
        var result = Method.Invoke(target, parameters);
        if (result is Task taskResult)
        {
            var tcs = new TaskCompletionSource<object>();
            if (taskResult.GetType().IsGenericType)
            {
                var taskResultType = taskResult.GetType().GetGenericArguments()[0];
                var awaiter = taskResult.GetAwaiter();
                awaiter.OnCompleted(() =>
                {
                    var resultProperty = taskResult.GetType().GetProperty("Result");
                    result = resultProperty?.GetValue(taskResult)!;
                    tcs.SetResult(result);
                });
                return tcs.Task;
            }

            await taskResult;
            tcs.SetResult(null!);

            return tcs.Task;
        }

        return Task.FromResult(result);
    }
}
