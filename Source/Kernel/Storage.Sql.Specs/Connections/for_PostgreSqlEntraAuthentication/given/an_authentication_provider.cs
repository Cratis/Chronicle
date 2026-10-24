// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Core;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_PostgreSqlEntraAuthentication.given;

public class an_authentication_provider : Specification
{
    internal RecordingCredential Credential;
    internal TimeProvider Clock;
    internal DateTimeOffset Now;
    internal PostgreSqlEntraAuthentication Authentication;

    void Establish()
    {
        Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        Clock = Substitute.For<TimeProvider>();
        Clock.GetUtcNow().Returns(_ => Now);
        Credential = new() { Acquire = _ => ValueTask.FromResult(new AccessToken($"token-{Now:O}", Now.AddHours(1))) };
        Authentication = new(Credential, Clock);
    }

    void Destroy() => Authentication.Dispose();
}
