// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_response_defaults;

public class when_authorization_is_read_by_a_proto3_client : Specification
{
    bool[] _authorized;
    bool[] _denied;

    void Because()
    {
        _authorized = [ReadVerdict(new CommandResult()), ReadVerdict(new CommandResult<string>()), ReadVerdict(new QueryResult<string>())];
        _denied = [ReadVerdict(new CommandResult { IsAuthorized = false }), ReadVerdict(new CommandResult<string> { IsAuthorized = false }), ReadVerdict(new QueryResult<string> { IsAuthorized = false })];
    }

    [Fact] void should_send_true_for_every_authorized_envelope() => _authorized.ShouldEqual([true, true, true]);
    [Fact] void should_preserve_every_denied_verdict() => _denied.ShouldEqual([false, false, false]);

    static bool ReadVerdict<T>(T result)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, result);
        stream.Position = 0;
        return Serializer.Deserialize<Proto3Envelope>(stream).IsAuthorized;
    }

    /// <summary>
    /// Models a proto3 client, whose missing boolean starts at false irrespective of C# initializers.
    /// </summary>
    [ProtoContract]
    class Proto3Envelope
    {
        [ProtoMember(2)]
        public bool IsAuthorized { get; set; }
    }
}
