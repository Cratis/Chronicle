// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Cratis.Chronicle.EventSequences;
using Cratis.Metrics;
using ClientEventSequence = Client::Cratis.Chronicle.EventSequences.EventSequence;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Binds real production entry points once, outside measurement. The same adapter also runs on main,
/// where metric recording and client span methods are generated static extensions rather than instances.
/// </summary>
static class TelemetryHotPathAdapter
{
    const BindingFlags Methods = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly string[] _routingValues = ["store", "namespace", "log", "customer"];

    /// <summary>Creates the production metric scope using the API available in this revision.</summary>
    /// <typeparam name="T">The meter owner.</typeparam>
    /// <param name="meter">The listener-backed meter.</param>
    /// <param name="factory">The scope factory type and method.</param>
    /// <param name="mode">The naming mode, if supported.</param>
    /// <param name="arguments">The activation-time scope dimensions.</param>
    /// <returns>The production metric scope.</returns>
    internal static object CreateScope<T>(System.Diagnostics.Metrics.Meter meter, (string Type, string Method) factory, string mode, params object[] arguments)
    {
        var method = typeof(EventSequence).Assembly.GetType(factory.Type)!.GetMethod(factory.Method, Methods)!;
        var parameters = method.GetParameters();
        var values = new List<object> { new Meter<T>(meter) };
        values.AddRange(arguments);
        if (parameters.Length > values.Count)
        {
            values.Add(Enum.Parse(parameters[^1].ParameterType, mode));
        }

        return method.Invoke(null, values.ToArray())!;
    }

    /// <summary>Binds a production record method without reflection in the timed path.</summary>
    /// <typeparam name="T">The recording delegate type.</typeparam>
    /// <param name="scope">The production metric scope.</param>
    /// <param name="typeName">The production metrics type.</param>
    /// <param name="methodName">The recording method.</param>
    /// <returns>The bound recording delegate.</returns>
    internal static T BindMetric<T>(object scope, string typeName, string methodName)
        where T : Delegate => typeof(EventSequence).Assembly.GetType(typeName)!.GetMethod(methodName, Methods)!.CreateDelegate<T>(scope);

    /// <summary>Compiles a call to the production append span method for this revision.</summary>
    /// <param name="source">The activity source.</param>
    /// <returns>The bound production span starter.</returns>
    internal static Func<IDisposable> BindAppend(ActivitySource source)
    {
        var assembly = typeof(ClientEventSequence).Assembly;
        var method = assembly.GetType("Cratis.Chronicle.EventSequences.EventSequenceTraces")!.GetMethod("Append", Methods)!;
        var parameters = method.GetParameters();
        var wrapper = new Cratis.Traces.ActivitySource<ClientEventSequence>(source);
        var arguments = new List<Expression> { Expression.Constant(wrapper, parameters[0].ParameterType) };
        if (parameters[1].ParameterType == typeof(string))
        {
            arguments.AddRange(_routingValues.Select(Expression.Constant));
        }
        else
        {
            var context = Activator.CreateInstance(parameters[1].ParameterType, "store", "namespace", "log", "customer");
            arguments.Add(Expression.Constant(context, parameters[1].ParameterType));
            arguments.Add(Expression.Constant(Enum.Parse(parameters[2].ParameterType, "Legacy")));
            arguments.Add(Expression.Constant(null, typeof(string)));
        }

        return Expression.Lambda<Func<IDisposable>>(Expression.Convert(Expression.Call(method, arguments), typeof(IDisposable))).Compile();
    }
}
