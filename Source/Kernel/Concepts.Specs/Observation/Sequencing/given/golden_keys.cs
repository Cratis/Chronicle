// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.given;

public static class golden_keys
{
    public static SequencingKey Key(int index) => index switch
    {
        0 => SequencingKey.ForEventSourceId("abc"),
        1 => SequencingKey.ForEventSourceId(EventSourceId.Unspecified),
        2 => SequencingKey.ForEventSourceId("\u00e9"),
        3 => SequencingKey.ForEventSourceId("e\u0301"),
        4 => SequencingKey.ForEventSourceId("\U0001F600"),
        5 => new ValueSequencingKey(["ab", "c"]),
        6 => new ValueSequencingKey(["a", "bc"]),
        7 => SequencingKey.ForEventSourceId(new string('a', 8187)),
        8 => SequencingKey.Unresolved("some-event-type"),
        9 => SequencingKey.Unresolved(EventTypeId.Unknown),
        10 => SequencingKey.Unresolved("utf16:d8000078"),
        11 => SequencingKey.Unresolved("\uD800x"),
        12 => SequencingKey.Unresolved(new string('t', 8186)),
        13 => SequencingKey.Unresolved(new string('t', 8187)),
        14 => SequencingKey.Unresolved("\uD800" + new string('t', 9000)),
        _ => throw new InvalidSequencingKey("Unknown golden vector.")
    };

    public static TheoryData<int, string, string, uint, ushort> TheoryRows()
    {
        var rows = new TheoryData<int, string, string, uint, ushort>();
        foreach (var row in Rows())
        {
            rows.Add((int)row[0], (string)row[1], (string)row[2], (uint)row[3], (ushort)row[4]);
        }

        return rows;
    }

    public static IEnumerable<object[]> Rows()
    {
        yield return [0, "0000000003616263", "063b1397a169a2f0e939bdad11ac2df004eda61bbcf96888f4fbe19643020e83", 0xd329e034U, (ushort)52];
        yield return [1, "0000000000", "8855508aade16ec573d21e6a485dfd0a7624085c1a14b5ecdd6485de0c6839a4", 0x1295514dU, (ushort)333];
        yield return [2, "0000000002c3a9", "6eea664fc33d1397ba3431459c243a2824c7c5618de309e275c418816f54864d", 0xf8306fb4U, (ushort)948];
        yield return [3, "000000000365cc81", "b43184b114fdd2b83d0adae16db99fc27424a1174668b278c3462ff998b9c704", 0x7cfba941U, (ushort)321];
        yield return [4, "0000000004f09f9880", "8c47507a2093fbc8d3c65590d90ae921bfc08c637fa7717915fc2cac810e3de8", 0x4564105fU, (ushort)95];
        yield return [5, "000000000261620000000163", "cc081c7b9d033c13d080d085f55e83ea76aadd31559dbcc0dad27c11dac1f7c2", 0x3ecb028bU, (ushort)651];
        yield return [6, "000000000161000000026263", "0df0def1e513e0198f38b4e158c043624d06a960584521e93cad6f7f82426d12", 0x6192a8cfU, (ushort)207];
        yield return [7, "0000001ffb" + string.Concat(Enumerable.Repeat("61", 8187)), "2d7ebb8910027113da357e37dac259f7e74669b0784019068d5666117b363be9", 0x003df9a7U, (ushort)423];
        yield return [8, "01000000000f736f6d652d6576656e742d74797065", "38600aa47d4e2424e4ea5256f093e7285588d9ed158d940ba5bb300a3e959be4", 0x3850194cU, (ushort)332];
        yield return [9, "010000000000", "36c159d76b52e03e496363607c5940227f62f4e048df344b050159bc97bba3d7", 0x392ac360U, (ushort)864];
        yield return [10, "01000000000e75746631363a6438303030303738", "b2797c37a442d2d428cd9a1648b80dae0aa2cb2ce3b59ea85554a52ca9825965", 0xeb6973dcU, (ushort)988];
        yield return [11, "01019466fd0a01cfa0678128b33a9829115f73a1845ab4aa3f5c9da6fe3c6e400db8", "2d3a3b9ebfb11e587347cf391cc38efe7617e1ee258490d7dbbdb36b2cb1ab7b", 0x261ba873U, (ushort)115];
        yield return [12, "010000001ffa" + string.Concat(Enumerable.Repeat("74", 8186)), "80a73652244080a7b1837f0c1ed657e1bd30edd60b93725099090dd748555328", 0xb5101a5aU, (ushort)602];
        yield return [13, "0101842aa91ac56172837e542f4bd8b6d0a1798d5a1e562613ed8bb8ca06d98d3668", "330735c65d062facec68aa3ea003b872abf7cc90c5df65f83da6fea8350b7a8e", 0xda1b60f7U, (ushort)247];
        yield return [14, "0101d6bd5b1f1080fe22e7cbc9c5aa1bf70cd9b04f7cda4db6e6d66d85dcc1abbd83", "add24d894707c624d512988580f65d91c93683c8366bb0f5ce285e0a00e15115", 0xc60a2dfeU, (ushort)510];
    }
}
