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
    /// <returns>Whether the pass was cut short by the budget.</returns>
    public static Task<bool> Run(Assembly assembly, int iterations, string filter) =>
        Run(assembly.GetTypes().Where(_ => _.IsSubclassOf(typeof(StorageBenchmark)) && !_.IsAbstract), iterations, filter);

    /// <summary>Runs selected fixture types without starting BenchmarkDotNet.</summary>
    /// <param name="types">The fixture types.</param>
    /// <param name="iterations">The number of measured iterations.</param>
    /// <param name="filter">The method filter.</param>
    /// <returns>Whether the budget cut the pass short.</returns>
    internal static async Task<bool> Run(IEnumerable<Type> types, int iterations, string filter)
    {
        try
        {
            await RunTypes(types, iterations, filter);
            return false;
        }
        catch (Exception error) when (error.GetBaseException() is BudgetExceededException)
        {
            return true;
        }
    }

    /// <summary>
    /// Gets every combination of the parameter values of a benchmark type. The parameter sources are public static
    /// properties and several (such as the providers) are declared on the base class, so the lookup asks for static
    /// members up the hierarchy or it finds nothing.
    /// </summary>
    /// <param name="type">The benchmark type.</param>
    /// <returns>The combinations.</returns>
    internal static List<List<(PropertyInfo Property, object Value)>> Combinations(Type type)
    {
        var combinations = new List<List<(PropertyInfo, object)>> { new() };
        foreach (var property in type.GetProperties().Where(_ => Attribute.IsDefined(_, typeof(ParamsSourceAttribute))))
        {
            var sourceName = property.GetCustomAttribute<ParamsSourceAttribute>()!.Name;
            var values = ((System.Collections.IEnumerable)type.GetProperty(sourceName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!.GetValue(null)!).Cast<object>().ToArray();
            combinations = [.. combinations.SelectMany(existing => values.Select(value => new List<(PropertyInfo, object)>([.. existing, (property, value)])))];
        }

        return combinations;
    }

    static async Task RunTypes(IEnumerable<Type> types, int iterations, string filter)
    {
        var selector = new Regex("^" + Regex.Escape(filter).Replace("\\*", ".*", StringComparison.Ordinal) + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        foreach (var type in types.OrderBy(_ => _.FullName, StringComparer.Ordinal))
        {
            var methods = type.GetMethods().Where(_ => Attribute.IsDefined(_, typeof(BenchmarkAttribute)) && selector.IsMatch($"{type.FullName}.{_.Name}")).ToArray();
            if (methods.Length == 0) continue;
            foreach (var combination in Combinations(type))
            {
                RunBudget.Current.ThrowIfExpired();
                var provider = combination.Where(_ => _.Value is StorageProvider).Select(_ => (StorageProvider?)_.Value).FirstOrDefault();
                var measured = methods.Where(_ => provider is null || KnownGaps.ReasonFor(provider.Value, _.Name) is null).ToArray();
                if (measured.Length == 0) continue;
                await RunCase(type, combination, measured, iterations);
            }
        }
    }

    static async Task RunCase(Type type, IReadOnlyList<(PropertyInfo Property, object Value)> combination, MethodInfo[] methods, int iterations)
    {
        var instance = Activator.CreateInstance(type)!;
        foreach (var (property, value) in combination) property.SetValue(instance, value);
        try
        {
            await Invoke(instance, type.GetMethods().Where(_ => Attribute.IsDefined(_, typeof(GlobalSetupAttribute))));
            using (Measurements.Counting())
            {
                foreach (var method in methods)
                {
                    for (var iteration = 0; iteration < iterations; iteration++)
                    {
                        RunBudget.Current.ThrowIfExpired();
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
        catch (Exception error) when (error.GetBaseException() is BudgetExceededException)
        {
            throw;
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
}
