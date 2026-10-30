// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading_the_kernel;

public class and_comparing_with_the_committed_contracts : given.the_kernel_inventory
{
    string[] _differences;
    string _actual;

    void Because()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "GrainPayloadContracts.txt");
        var received = Path.Combine(AppContext.BaseDirectory, "GrainPayloadContracts.received.txt");
        var expected = File.Exists(path) ? File.ReadAllText(path).ReplaceLineEndings("\n") : string.Empty;
        _actual = _manifest.ToText();
        if (_actual != expected)
        {
            File.WriteAllText(received, _actual);
        }

        _differences = expected == _actual ? [] : [$"Copy {received} and review the diff.", .. expected.Split('\n').Except(_actual.Split('\n')).Select(_ => "- " + _), .. _actual.Split('\n').Except(expected.Split('\n')).Select(_ => "+ " + _)];
    }

    [Fact] void should_match_the_committed_contracts() => _differences.ShouldBeEmpty();
    [Fact] void should_be_deterministic_regardless_of_root_order() => GenerateAgain().ToText().ShouldEqual(_actual);
    [Fact] void should_not_be_empty() => _manifest.Types.ShouldNotBeEmpty();
}
