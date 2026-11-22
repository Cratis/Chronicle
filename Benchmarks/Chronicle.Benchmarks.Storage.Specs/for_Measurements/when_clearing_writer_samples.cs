// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_Measurements;

[Collection(nameof(StorageRunOptionsCollection))]
public class when_clearing_writer_samples : Specification
{
    readonly string _path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-samples.json");
    JsonElement[] _samples = [];

    void Establish()
    {
        Measurements.Clear();
        using var scope = Measurements.Counting();
        Measurements.Record("previous-provider", new(1, 1, new(0, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(1)], 0)
        {
            Samples = [new(0, 0, 0, 1, true)]
        });
    }

    void Because()
    {
        Measurements.Clear();
        Measurements.WriteSamples(_path);
        using var document = JsonDocument.Parse(File.ReadAllText(_path));
        _samples = document.RootElement.EnumerateArray().ToArray();
    }

    void Destroy() => File.Delete(_path);

    [Fact] void should_not_export_the_previous_provider_samples() => _samples.ShouldBeEmpty();
}
