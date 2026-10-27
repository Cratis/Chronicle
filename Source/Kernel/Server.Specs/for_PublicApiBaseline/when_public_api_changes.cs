// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// Freezes every exported Contracts declaration, without starting the kernel or invoking handlers.
/// </summary>
public class when_public_api_changes : Specification
{
    string _current = string.Empty;
    string? _baseline;

    void Because()
    {
        var contracts = typeof(IServices).Assembly;
        _current = given.public_api_snapshot.Render(contracts.GetExportedTypes().Where(type => type.Assembly == contracts));
        using var resource = typeof(when_public_api_changes).Assembly.GetManifestResourceStream("KernelContracts.public-api.txt");
        if (resource is not null)
        {
            using var reader = new StreamReader(resource);
            _baseline = reader.ReadToEnd();
        }
    }

    [Fact]
    void should_match_the_committed_public_api()
    {
        try
        {
            given.public_api_difference.RequireMatch(_baseline, _current);
        }
        catch (InvalidOperationException exception)
        {
            var receivedPath = Path.Combine(AppContext.BaseDirectory, "KernelContracts.public-api.received.txt");
            File.WriteAllText(receivedPath, _current, new System.Text.UTF8Encoding(false));
            throw new InvalidOperationException($"{exception.Message}\nCurrent snapshot: {receivedPath}", exception);
        }
    }
}
