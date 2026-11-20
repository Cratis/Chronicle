// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Counts round trips in a deterministic pass of its own, outside BenchmarkDotNet: every benchmark case runs its
/// setup, then each benchmark method exactly the profile's iteration count of times, all inside a
/// <see cref="Measurements.Counting"/> scope. BenchmarkDotNet is used for timing only, so its jitting, pilot and
/// warmup invocations can never be counted, and a failed iteration is counted as a failure of exactly that iteration.
/// </summary>
public static class CountedPass
{
    /// <summary>Runs the counted pass for every benchmark case the filter selects.</summary>
    /// <param name="assembly">The assembly holding the benchmarks.</param>
    /// <param name="iterations">How many times each benchmark method runs.</param>
    /// <param name="filter">The BenchmarkDotNet style glob filter (`*` selects everything).</param>
    /// <returns>The task.</returns>
    public static async Task Run(Assembly assembly, int iterations, string filter)
    {
        var selector = new Regex("^" + Regex.Escape(filter).Replace("\\*", ".*", StringComparison.Ordinal) + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        foreach (var type in assembly.GetTypes().Where(_ => _.IsSubclassOf(typeof(StorageBenchmark)) && !_.IsAbstract).OrderBy(_ => _.FullName, StringComparer.Ordinal))
        {
            var methods = type.GetMethods().Where(_ => Attribute.IsDefined(_, typeof(BenchmarkAttribute)) && selector.IsMatch($"{type.FullName}.{_.Name}")).ToArray();
            if (methods.Length == 0) continue;
            foreach (var combination in Combinations(type))
            {
                await RunCase(type, combination, methods, iterations);
            }
        }
    }

    static async Task RunCase(Type type, IReadOnlyList<(PropertyInfo Property, object Value)> combination, MethodInfo[] methods, int iterations)
    {
        var instance = Activator.CreateInstance(type)!;
        foreach (var (property, value) in combination) property.SetValue(instance, value);
        await Invoke(instance, type.GetMethods().Where(_ => Attribute.IsDefined(_, typeof(GlobalSetupAttribute))));
        try
        {
            using (Measurements.Counting())
            {
                foreach (var method in methods)
                {
                    for (var iteration = 0; iteration < iterations; iteration++)
                    {
                        await InvokeCounted(instance, method);
                    }
                }
            }
        }
        finally
        {
            await Invoke(instance, type.GetMethods().Where(_ => Attribute.IsDefined(_, typeof(GlobalCleanupAttribute))));
        }
    }

    static async Task InvokeCounted(object instance, MethodInfo method)
    {
        try
        {
            await Invoke(instance, [method]);
        }
        catch (Exception error)
        {
            // The benchmark has already counted the failure of this iteration; the pass carries on with the next one.
            await Console.Error.WriteLineAsync($"Counted iteration of {method.DeclaringType?.Name}.{method.Name} failed: {error.GetBaseException().Message}");
        }
    }

    static async Task Invoke(object instance, IEnumerable<MethodInfo> methods)
    {
        foreach (var method in methods)
        {
            var result = method.Invoke(instance, null);
            if (result is Task task) await task;
        }
    }

    static List<List<(PropertyInfo Property, object Value)>> Combinations(Type type)
    {
        var combinations = new List<List<(PropertyInfo, object)>> { new() };
        foreach (var property in type.GetProperties().Where(_ => Attribute.IsDefined(_, typeof(ParamsSourceAttribute))))
        {
            var sourceName = property.GetCustomAttribute<ParamsSourceAttribute>()!.Name;
            var values = ((System.Collections.IEnumerable)type.GetProperty(sourceName)!.GetValue(null)!).Cast<object>().ToArray();
            combinations = [.. combinations.SelectMany(existing => values.Select(value => new List<(PropertyInfo, object)>([.. existing, (property, value)])))];
        }

        return combinations;
    }
}
