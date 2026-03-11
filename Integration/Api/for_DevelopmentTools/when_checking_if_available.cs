// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_checking_if_available.context;

namespace Cratis.Chronicle.Integration.Api.for_DevelopmentTools;

/// <summary>
/// Verifies that the development-tools availability endpoint responds successfully.
/// This test passes only when the Chronicle server is built in Development (Debug) mode.
/// </summary>
/// <param name="context">The spec context.</param>
[Collection(ChronicleCollection.Name)]
public class when_checking_if_available(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        // CA2213: Result is disposed by the base class lifecycle; HttpResponseMessage does not hold critical resources in tests.
#pragma warning disable CA2213
        public HttpResponseMessage Result = null!;
#pragma warning restore CA2213

        async Task Because() =>
            Result = await Client.GetAsync("/api/development-tools/is-available");
    }

    [Fact] void should_return_success() => Context.Result.IsSuccessStatusCode.ShouldBeTrue();
}
