// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Dynamic;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator;

/// <summary>
/// Pins the bytes and hashes captured by running the pre-Stage-A implementation.
/// These persisted representations must not change when optimizing the encoder.
/// </summary>
public class when_calculating_hash_for_the_canonical_compatibility_corpus : Specification
{
    /// <summary>
    /// Each shape has generation 1, 2 and 3, in that order. Bytes are base64 to preserve every escape exactly.
    /// </summary>
    static readonly (string Bytes, string Hash)[] _expected =
    [
        ("eyJhIjo0MiwiZmxhZyI6ZmFsc2UsIm1pc3NpbmciOm51bGwsInoiOiJsYXN0In0=", "SvEXmp3tgV8ShStnDInlySx1bW9maggnIaAnO3QW08o="),
        ("eyJwcmV2aW91cyI6eyJhIjo0MiwiZmxhZyI6ZmFsc2UsIm1pc3NpbmciOm51bGwsInoiOiJsYXN0In0sInZlcnNpb24iOjJ9", "vhiHp++UJdywa1JbR6xxZYb8nSzkZopTK9rgmwNLe/o="),
        ("eyJhZGRlZCI6W10sInByZXZpb3VzIjp7InByZXZpb3VzIjp7ImEiOjQyLCJmbGFnIjpmYWxzZSwibWlzc2luZyI6bnVsbCwieiI6Imxhc3QifSwidmVyc2lvbiI6Mn0sInZlcnNpb24iOjN9", "dvXYJzVHX/y/IJoO6UsZXGJ76n3ItpbNhT7s0kOgYvU="),
        ("eyJhIjp0cnVlLCJ6Ijp7ImEiOnsiYyI6MywiZCI6NH0sImIiOjJ9fQ==", "jcR3hakxFJ3/3p7hmMD6CBmt+NRCyAtIIGRrXCiypRM="),
        ("eyJwcmV2aW91cyI6eyJhIjp0cnVlLCJ6Ijp7ImEiOnsiYyI6MywiZCI6NH0sImIiOjJ9fSwidmVyc2lvbiI6Mn0=", "Y2uhxk7BUFZuKLlQFxGv7SgNx6N0u1pmKBuuWF3p1pc="),
        ("eyJhZGRlZCI6W10sInByZXZpb3VzIjp7InByZXZpb3VzIjp7ImEiOnRydWUsInoiOnsiYSI6eyJjIjozLCJkIjo0fSwiYiI6Mn19LCJ2ZXJzaW9uIjoyfSwidmVyc2lvbiI6M30=", "vwjASrjtdHmL1fGFM+6t4qKTfd1gGZq2bahcaZpbXDQ="),
        ("eyJlbXB0eSI6W10sIm1peGVkIjpbbnVsbCx0cnVlLGZhbHNlLCJ0ZXh0IiwzLHsiYSI6MSwieiI6Mn0sWzMsMiwxXV19", "QAyUnk9zMj2mQrLZpONjqHX/yKfM16Lumt0ri2k/Kis="),
        ("eyJwcmV2aW91cyI6eyJlbXB0eSI6W10sIm1peGVkIjpbbnVsbCx0cnVlLGZhbHNlLCJ0ZXh0IiwzLHsiYSI6MSwieiI6Mn0sWzMsMiwxXV19LCJ2ZXJzaW9uIjoyfQ==", "Vdg4cwLL6hobM4a+yScnFY6cFnZOVRJT+YOiWAeeYzs="),
        ("eyJhZGRlZCI6W10sInByZXZpb3VzIjp7InByZXZpb3VzIjp7ImVtcHR5IjpbXSwibWl4ZWQiOltudWxsLHRydWUsZmFsc2UsInRleHQiLDMseyJhIjoxLCJ6IjoyfSxbMywyLDFdXX0sInZlcnNpb24iOjJ9LCJ2ZXJzaW9uIjozfQ==", "+PFzVOHCncyUudVMhu+GrH5JRAGWwNWm9xlbWC6NiYc="),
        ("e30=", "7Nf/vjcwrnZS9421yUab6P59N35cxq7fQYrHbYzG/XQ="),
        ("eyJwcmV2aW91cyI6e30sInZlcnNpb24iOjJ9", "AU9lwfn06FGOzht0soq3Qtnb8dDmGjPlM2wkLUhS2rs="),
        ("eyJhZGRlZCI6W10sInByZXZpb3VzIjp7InByZXZpb3VzIjp7fSwidmVyc2lvbiI6Mn0sInZlcnNpb24iOjN9", "t+6tAltzftc0U1Qqkrqgbh18Fu9bG9ojY3moRtUOB7U="),
        ("eyJkZWNpbWFsIjoxMjMuNDUwMCwiZG91YmxlIjotMy4xNDE1OTI2NTM1ODk3OTMsImV4cG9uZW50IjoxRSsxMDAsImxhcmdlIjoxODQ0Njc0NDA3MzcwOTU1MTYxNSwibmVnYXRpdmUiOi05MjIzMzcyMDM2ODU0Nzc1ODA4LCJzbWFsbCI6MUUtMTAwLCJ6ZXJvIjotMH0=", "AaNok61yVyI8XaTR870tccRs3iSxIRpqDcc/zhXqUms="),
        ("eyJwcmV2aW91cyI6eyJkZWNpbWFsIjoxMjMuNDUwMCwiZG91YmxlIjotMy4xNDE1OTI2NTM1ODk3OTMsImV4cG9uZW50IjoxRSsxMDAsImxhcmdlIjoxODQ0Njc0NDA3MzcwOTU1MTYxNSwibmVnYXRpdmUiOi05MjIzMzcyMDM2ODU0Nzc1ODA4LCJzbWFsbCI6MUUtMTAwLCJ6ZXJvIjotMH0sInZlcnNpb24iOjJ9", "yw+yteuhERpWCR8ZrcjqtMB8S+bF4JXsgw3b3+m+JLA="),
        ("eyJhZGRlZCI6W10sInByZXZpb3VzIjp7InByZXZpb3VzIjp7ImRlY2ltYWwiOjEyMy40NTAwLCJkb3VibGUiOi0zLjE0MTU5MjY1MzU4OTc5MywiZXhwb25lbnQiOjFFKzEwMCwibGFyZ2UiOjE4NDQ2NzQ0MDczNzA5NTUxNjE1LCJuZWdhdGl2ZSI6LTkyMjMzNzIwMzY4NTQ3NzU4MDgsInNtYWxsIjoxRS0xMDAsInplcm8iOi0wfSwidmVyc2lvbiI6Mn0sInZlcnNpb24iOjN9", "/sQuiw6Uh6nwlHivgCT3tF8t/4r1DfDTcQpfezM+dCM="),
        ("eyJzdXJyb2dhdGVzIjoi77+9fO+/vSIsInRleHQiOiI8PiYnXCJcXFxiXGZcblxyXHRcdTAwMDBcdTAwMUYgYmzDpSDml6XmnKzoqp4gXHVEODNEXHVERTgwIFx1MjAyOFx1MjAyOSJ9", "8UalqZ+XGfjQsTYueLtfLWrvIuCbeLZPst9xAdYIbpc="),
        ("eyJwcmV2aW91cyI6eyJzdXJyb2dhdGVzIjoi77+9fO+/vSIsInRleHQiOiI8PiYnXCJcXFxiXGZcblxyXHRcdTAwMDBcdTAwMUYgYmzDpSDml6XmnKzoqp4gXHVEODNEXHVERTgwIFx1MjAyOFx1MjAyOSJ9LCJ2ZXJzaW9uIjoyfQ==", "U1zfBsXLLk1g2q+bu4IZ1u2sqV8dxeowS98YD4BPYZY="),
        ("eyJhZGRlZCI6W10sInByZXZpb3VzIjp7InByZXZpb3VzIjp7InN1cnJvZ2F0ZXMiOiLvv71877+9IiwidGV4dCI6Ijw+JidcIlxcXGJcZlxuXHJcdFx1MDAwMFx1MDAxRiBibMOlIOaXpeacrOiqniBcdUQ4M0RcdURFODAgXHUyMDI4XHUyMDI5In0sInZlcnNpb24iOjJ9LCJ2ZXJzaW9uIjozfQ==", "dERyaYTbKUEoEgYCNqiXSbiitxeoPY4rk4/vjriAWsE="),
        ("eyIiOiJlbXB0eSIsImEiOiJwcmVmaXgiLCJhXHUwMDAwIjoiY29udHJvbCIsImFcXFwiXG4iOiJlc2NhcGVkIiwiw6kiOiJhY2NlbnQiLCJcdUQ4M0RcdURFODAiOiJwYWlyIiwiXHVFMDAwIjoiQk1QIn0=", "gUuGtuqBPawocTJhwseUTx/Zw0nd0x71YTq8n7qCxZI="),
        ("eyJwcmV2aW91cyI6eyIiOiJlbXB0eSIsImEiOiJwcmVmaXgiLCJhXHUwMDAwIjoiY29udHJvbCIsImFcXFwiXG4iOiJlc2NhcGVkIiwiw6kiOiJhY2NlbnQiLCJcdUQ4M0RcdURFODAiOiJwYWlyIiwiXHVFMDAwIjoiQk1QIn0sInZlcnNpb24iOjJ9", "33xqHWzHcsSPpba52AUlXEgKa4X4AJFD0leOGSXw5Tk="),
        ("eyJhZGRlZCI6W10sInByZXZpb3VzIjp7InByZXZpb3VzIjp7IiI6ImVtcHR5IiwiYSI6InByZWZpeCIsImFcdTAwMDAiOiJjb250cm9sIiwiYVxcXCJcbiI6ImVzY2FwZWQiLCLDqSI6ImFjY2VudCIsIlx1RDgzRFx1REU4MCI6InBhaXIiLCJcdUUwMDAiOiJCTVAifSwidmVyc2lvbiI6Mn0sInZlcnNpb24iOjN9", "1EbVSt/vyMwrc/i3WBQHiCyPiXpUTPSnrAqdqG8mexc="),
        ("eyJkdXBsaWNhdGVzIjp7IloiOjQsImEiOjEsImEiOjIsImEiOjMsInoiOjB9fQ==", "x7jpwpBJflK06gB8fA8ojuxsoVHbalminRb2jggoUgs="),
        ("eyJwcmV2aW91cyI6eyJkdXBsaWNhdGVzIjp7IloiOjQsImEiOjEsImEiOjIsImEiOjMsInoiOjB9fSwidmVyc2lvbiI6Mn0=", "dCpScGOt1uhwwZyNwb0QUqBZJDM0IJKRjX09QcIY2Z0="),
        ("eyJhZGRlZCI6W10sInByZXZpb3VzIjp7InByZXZpb3VzIjp7ImR1cGxpY2F0ZXMiOnsiWiI6NCwiYSI6MSwiYSI6MiwiYSI6MywieiI6MH19LCJ2ZXJzaW9uIjoyfSwidmVyc2lvbiI6M30=", "fX8FQG2ZIa/QfHIMQh8hMUV9q1GkL+3gBMPCaH9QZCs=")
    ];

    (string Bytes, string Hash)[] _single;
    (string Bytes, string Hash)[] _three;
    ExpandoObject[][] _corpus;

    void Establish() => _corpus = given.canonical_event_corpus.Generations().ToArray();

    void Because()
    {
        _single = _corpus.Select(generations => Calculate(generations[0])).ToArray();
        _three = _corpus.SelectMany(generations => generations.Select(Calculate)).ToArray();
    }

    [Fact] void should_preserve_every_byte_for_one_generation() => _single.Select(result => result.Bytes).SequenceEqual(_expected.Where((_, index) => index % 3 == 0).Select(result => result.Bytes)).ShouldBeTrue();
    [Fact] void should_preserve_every_hash_for_one_generation() => _single.Select(result => result.Hash).SequenceEqual(_expected.Where((_, index) => index % 3 == 0).Select(result => result.Hash)).ShouldBeTrue();
    [Fact] void should_preserve_every_byte_for_three_generations() => _three.Select(result => result.Bytes).SequenceEqual(_expected.Select(result => result.Bytes)).ShouldBeTrue();
    [Fact] void should_preserve_every_hash_for_three_generations() => _three.Select(result => result.Hash).SequenceEqual(_expected.Select(result => result.Hash)).ShouldBeTrue();

    static (string Bytes, string Hash) Calculate(ExpandoObject content)
    {
        var buffer = new ArrayBufferWriter<byte>();
        CanonicalJson.Write(content, buffer);
        var hash = new EventHashCalculator().Calculate(given.canonical_event_corpus.EventType, given.canonical_event_corpus.EventSource, content);
        return (Convert.ToBase64String(buffer.WrittenSpan), hash.Value);
    }
}
