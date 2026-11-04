// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;
using Cratis.Chronicle.Integration.for_PerProcessActivation;

namespace Cratis.Chronicle.Integration;

public static class Program
{
    static readonly IReadOnlyDictionary<string, Func<string[], Task<int>>> _children = new Dictionary<string, Func<string[], Task<int>>>
    {
        [PerProcessApplication.Verb] = PerProcessApplication.Run,
        [OccurrenceApplication.HolderVerb] = args => OccurrenceApplication.Run(args, hold: true),
        [OccurrenceApplication.RecoveryVerb] = args => OccurrenceApplication.Run(args, hold: false)
    };

    public static Task<int> Main(string[] args) => args.Length > 0 && _children.TryGetValue(args[0], out var child)
        ? child(args[1..])
        : global::Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args);
}
