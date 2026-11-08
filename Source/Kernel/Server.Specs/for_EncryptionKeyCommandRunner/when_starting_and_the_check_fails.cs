// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_starting_and_the_check_fails : given.a_check
{
    int?[] _exits = [];

    async Task Because()
    {
        Exception[] failures =
        [
            new InvalidOperationException(),
            new TimeoutException(),
            new HttpRequestException(),
            new TaskCanceledException(),
            new OperationCanceledException()
        ];
        var exits = new List<int?>();
        foreach (var failure in failures)
        {
            _check.Inspect(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns<Task<Cratis.Chronicle.Storage.Compliance.EncryptionKeyStoreUpgradeStatus>>(_ => throw failure);
            exits.Add(await Run(false));
        }

        _exits = [.. exits];
    }

    [Fact] void should_refuse_to_start_for_every_failure() => (_exits.Length == 5 && _exits.All(exit => exit == 4)).ShouldBeTrue();
}
