// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Runs the selected cells serially, with fresh MongoDB state each time.</summary>
internal static class ProfileSweep
{
    /// <summary>Executes the repetitions and optional separate trace runs.</summary>
    /// <param name="options">The selection and workload options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Zero only when every repetition completes.</returns>
    /// <exception cref="HarnessFailure">The selection has no cells.</exception>
    public static async Task<int> Run(ProfileOptions options, CancellationToken cancellationToken)
    {
        var cells = ProfileMatrix.Select(options);
        if (cells.Length == 0)
        {
            throw new HarnessFailure("Cell filter matched no cells.");
        }
        var sha = await ProfileRepository.Sha(cancellationToken);
        var mongo = Environment.GetEnvironmentVariable("CHRONICLE_MONGODB_IMAGE") ?? "mongo:8.2";
        var session = new ProfileSession(Guid.NewGuid().ToString("N"), options.Results, sha, mongo);
        await DockerCli.Require(ImageCommands.Pull(mongo), cancellationToken);
        await DockerCli.Require(ImageCommands.Pull("busybox:1.36"), cancellationToken);
        var networkContext = ContainerLifecycle.Context(session, new RunIdentity("network", 0, false));
        var results = new List<ProfileResult>();
        try
        {
            await DockerCli.Require(ContainerCommands.Network(networkContext), cancellationToken);
            foreach (var cell in cells)
            {
                for (var repetition = 0; repetition < options.Repetitions + (options.GcTrace ? 1 : 0); repetition++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var trace = repetition == options.Repetitions;
                    var id = $"{cell.Id}-{repetition + 1}{(trace ? "-trace" : string.Empty)}";
                    Console.WriteLine($"Running {id}");
                    var context = ContainerLifecycle.Context(session, new RunIdentity(id, repetition + 1, trace));
                    results.Add(await ProfileRun.Execute(context, cell, options, cancellationToken));
                    await ResultSummary.Write(options.Results, results, cancellationToken);
                }
            }
        }
        finally
        {
            await DockerCli.Run(CleanupCommands.RemoveNetwork(networkContext.Network), CancellationToken.None);
        }
        return results.TrueForAll(result => result.Status == "completed") ? 0 : 1;
    }
}
