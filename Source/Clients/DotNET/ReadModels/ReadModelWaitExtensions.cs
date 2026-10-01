// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Helper extensions providing wait methods for read model instances.
/// </summary>
/// <remarks>
/// These extensions are very useful for integration testing purposes.
/// </remarks>
public static class ReadModelWaitExtensions
{
    const int DefaultDelay = 50;

    /// <summary>
    /// Wait till a set of read model instances, read by their keys, together satisfy a predicate, with an optional timeout.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="readModels">The <see cref="IReadModels"/> to read the instances from.</param>
    /// <param name="keys">The <see cref="ReadModelKey">keys</see> to get the instances for.</param>
    /// <param name="predicate">The predicate the instances must together satisfy.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>The instances once they satisfy the predicate.</returns>
    public static async Task<IReadOnlyList<TReadModel?>> WaitTillInstancesSatisfy<TReadModel>(this IReadModels readModels, IEnumerable<ReadModelKey> keys, Func<IReadOnlyList<TReadModel?>, bool> predicate, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var instances = new List<TReadModel?>();
            foreach (var key in keys)
            {
                instances.Add(await readModels.GetInstanceById<TReadModel>(key));
            }

            if (predicate(instances))
            {
                return instances;
            }

            await Task.Delay(DefaultDelay, cts.Token);
        }
    }
}
