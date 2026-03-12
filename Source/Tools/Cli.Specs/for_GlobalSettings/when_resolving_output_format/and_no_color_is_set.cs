// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_GlobalSettings.when_resolving_output_format;

[Collection(CliSpecsCollection.Name)]
public sealed class and_no_color_is_set : Specification, IDisposable
{
    string _previousNoColor;
    GlobalSettings _settings;
    string _result;

    void Establish()
    {
        _previousNoColor = Environment.GetEnvironmentVariable("NO_COLOR");
        Environment.SetEnvironmentVariable("NO_COLOR", "1");
        _settings = new GlobalSettings { Output = OutputFormats.Auto };
    }

    void Because() => _result = _settings.ResolveOutputFormat();

    [Fact] void should_return_plain() => _result.ShouldEqual(OutputFormats.Plain);

    /// <inheritdoc/>
    void IDisposable.Dispose() => Environment.SetEnvironmentVariable("NO_COLOR", _previousNoColor);
}
