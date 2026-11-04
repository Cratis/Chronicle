// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using SpecificationBase = Cratis.Specifications.Specification;

namespace Cratis.Chronicle.MongoDB.Integration.Generations.for_ReleasedKernelGenerationFloor.given;

/// <summary>
/// A released kernel image that populates its event stores and is stopped, after which the write witness is armed on every
/// existing database, optionally a floor is seeded, and the kernel is started again. Starting released kernels takes
/// minutes, so the run happens once per spec class and every fact reads the same outcome.
/// </summary>
/// <param name="image">The released kernel image.</param>
/// <param name="seedFloor">Whether to seed a floor above the binary's support before the second start.</param>
/// <param name="expectRefusal">Whether the second start is expected to exit instead of becoming healthy.</param>
[Collection(ReleasedFloorCollection.Name)]
public abstract class a_populated_store(string image, bool seedFloor, bool expectRefusal) : SpecificationBase
{
    static readonly ConcurrentDictionary<Type, Task<KernelOutcome>> _outcomes = new();

    protected KernelOutcome _outcome;

    async Task Establish() => _outcome = await _outcomes.GetOrAdd(GetType(), static (_, self) => self.Run(), this);

    async Task<KernelOutcome> Run()
    {
        await using var kernel = new ReleasedFloorKernel(image);
        await kernel.StartDatabase();
        await kernel.StartHealthy();
        await kernel.StopKernel();
        var witness = new EventStoreWriteWitness(kernel.Client);
        var seeded = seedFloor ? await kernel.SeedFloorInEveryStore() : [];
        await witness.Arm();
        if (expectRefusal)
        {
            await kernel.StartExpectingRefusal();
        }
        else
        {
            await kernel.StartHealthy();
        }
        var named = expectRefusal && (await kernel.LogsContain("KernelBelowGenerationFloor") || await kernel.LogsContain("below generation floor"));
        var writes = await WritesWithin(witness, expectRefusal ? TimeSpan.Zero : TimeSpan.FromSeconds(90));
        return new(seeded.Count, kernel.Exited, named, writes, (await witness.CreatedEventStoreDatabases()).Count);
    }

    static async Task<long> WritesWithin(EventStoreWriteWitness witness, TimeSpan bound)
    {
        var deadline = DateTimeOffset.UtcNow + bound;
        long writes;
        while ((writes = await witness.EventStoreWrites()) == 0 && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
        return writes;
    }
}
