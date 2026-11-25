// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator;

public class when_calculating_hash_for_extended_clr_values : Specification
{
    /// <summary>
    /// Captured from Stage A before introducing the direct CLR writer. These bytes are the contract,
    /// including BigInteger/concept objects, raw numeric tokens and replacement of invalid UTF-16.
    /// </summary>
    static readonly (string Bytes, string Hash)[] _expected =
    [
        ("eyJpbnRlZ2VycyI6WzI1NSwtMTI4LC0zMjc2OCw2NTUzNSwtMjE0NzQ4MzY0OCw0Mjk0OTY3Mjk1LC05MjIzMzcyMDM2ODU0Nzc1ODA4LDE4NDQ2NzQ0MDczNzA5NTUxNjE1LC0xNzAxNDExODM0NjA0NjkyMzE3MzE2ODczMDM3MTU4ODQxMDU3MjgsMzQwMjgyMzY2OTIwOTM4NDYzNDYzMzc0NjA3NDMxNzY4MjExNDU1XX0=", "uv4QVUluHWqk+1BNeJOMZ5ADmvAfOe2ZyyCZj43I8Fo="),
        ("eyJudW1iZXJzIjpbMUUtNDUsMy40MDI4MjM1RSszOCwtMCw1RS0zMjQsMS43OTc2OTMxMzQ4NjIzMTU3RSszMDgsLTAsMS41LC03OTIyODE2MjUxNDI2NDMzNzU5MzU0Mzk1MDMzNSw3OTIyODE2MjUxNDI2NDMzNzU5MzU0Mzk1MDMzNSwwLjAwMDAsMS4yMzAwXX0=", "WrPtr6LcBxczEgPsUZTFFNgQ3De1x+z56yIbxfII/Lc="),
        ("eyJiaWciOnsiSXNFdmVuIjp0cnVlLCJJc09uZSI6ZmFsc2UsIklzUG93ZXJPZlR3byI6ZmFsc2UsIklzWmVybyI6ZmFsc2UsIlNpZ24iOjF9LCJuZWdhdGl2ZSI6eyJJc0V2ZW4iOnRydWUsIklzT25lIjpmYWxzZSwiSXNQb3dlck9mVHdvIjpmYWxzZSwiSXNaZXJvIjpmYWxzZSwiU2lnbiI6LTF9fQ==", "qRcW0P16OQ78HkwWmYznSicloE2CFLVQAVEagqiaPgI="),
        ("eyJvZmZzZXQiOiIyMDI1LTAxLTAyVDAzOjA0OjA1LjEyMzQ1NjcrMDU6MzAiLCJ1bnNwZWNpZmllZCI6IjIwMjUtMDEtMDJUMDM6MDQ6MDUiLCJ1dGMiOiIyMDI1LTAxLTAyVDAzOjA0OjA1LjEyMzQ1NjdaIn0=", "mcKZpdBGtxZlYwxiYHmNi/UAkKWPcH2Qy8TIp5+FSEQ="),
        ("eyJkYXRlIjoiMjAyNS0wMS0wMiIsImd1aWQiOiIwMTIzNDU2Ny04OWFiLWNkZWYtMDEyMy00NTY3ODlhYmNkZWYiLCJzcGFuIjoiLTEuMTA6MTc6MzYuNzg5MDEyMyIsInRpbWUiOiIwMzowNDowNS4xMjM0NTY3In0=", "foea9a+LQfd+R1lYhGjKeItvktFNebX/A3hmoVGL0gU="),
        ("eyJjb25jZXB0Ijp7IklzU3BlY2lmaWVkIjp0cnVlLCJWYWx1ZSI6InNvdXJjZTw+JiJ9LCJlbnVtIjo1LCJuYW1lZCI6IlNlY29uZCIsInR5cGUiOnsiVmFsdWUiOiJ0eXBlIn19", "aOlFPOaawrKfxyO2W8HDXR4XIpwWPT/6mhC8XlibTXU="),
        ("eyJieXRlcyI6IkFBSCsvdz09IiwiZGljdGlvbmFyeSI6eyJhIjpbdHJ1ZSwiPD4mIix7ImEiOjEsImIiOjJ9XSwieiI6bnVsbH0sInR5cGVkIjpbMywyLDFdfQ==", "21pG7zUjRSNtqYmiZXSH1pKFULtbqppuQm7NRCiuCis="),
        ("eyJlbGVtZW50Ijp7ImEiOjFlLTA5LCJhIjoyLCJiIjotMC4wMH0sIm5vZGUiOnsiYSI6Ijw+Jlx1RDgzRFx1REU4MCIsInoiOltudWxsLHRydWUsMS4yMzAwLDFlKzEwXX19", "0Y4mANUihFDGC3YSPAHCSWsZk54FsCafyr9mQOxVodI="),
        ("eyJjaGFyIjoi77+9IiwidGV4dCI6Iu+/vXzvv718XHVEODNEXHVERTgwIiwi77+9IjoxLCLvv70iOjIsIu+/vSI6M30=", "5Uqt2Fh4C4S47YHkdWLiAk08Caw62us3+Agmd0qJK6g="),
        ("eyJjaGFyYWN0ZXJzIjpbIjwiLCJcIiIsIlxuIiwiw6kiXSwib2JqZWN0Ijp7IkEiOjQyLCJaIjoibGFzdCJ9fQ==", "qbYZsn5D+wTq1QM+y4VgRpWGL5y0YQuzxqpfLjeerCs=")
    ];

    (byte[] Bytes, string Hash)[] _actual;
    byte[][] _encoded;

    void Because()
    {
        var calculator = new EventHashCalculator();
        var contents = given.extended_event_corpus.Shapes().ToArray();
        _actual = contents.Select(content =>
        {
            var bytes = new ArrayBufferWriter<byte>();
            CanonicalJson.Write(content, bytes);
            return (bytes.WrittenSpan.ToArray(), calculator.Calculate(new("canonical-corpus"), new("source|é"), content).Value);
        }).ToArray();
        _encoded = contents.Select(content => System.Text.Encoding.UTF8.GetBytes(CanonicalJson.Encode(content))).ToArray();
    }

    [Fact] void should_preserve_every_canonical_byte() => _actual.Select(_ => Convert.ToBase64String(_.Bytes)).ShouldEqual(_expected.Select(_ => _.Bytes));
    [Fact] void should_preserve_every_hash() => _actual.Select(_ => _.Hash).ShouldEqual(_expected.Select(_ => _.Hash));
    [Fact] void should_preserve_the_string_encoding_path() => _encoded.Select(Convert.ToBase64String).ShouldEqual(_expected.Select(_ => _.Bytes));
}
