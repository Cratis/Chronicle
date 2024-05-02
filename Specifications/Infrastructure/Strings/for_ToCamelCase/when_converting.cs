// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Strings.for_ToCamelCase;

public static class when_converting
{
#pragma warning disable RCS1181
    // These test cases were copied from Json.NET via System.Text.Json in the DotNet framework.
#pragma warning restore RCS1181
    [Theory]
    [InlineData("URLValue", "URLValue")]
    [InlineData("URL", "URL")]
    [InlineData("ID", "ID")]
    [InlineData("i", "I")]
    [InlineData("", "")]
    [InlineData("😀葛🀄", "😀葛🀄")] // Surrogate pairs
    [InlineData("άλφαΒήταΓάμμα", "ΆλφαΒήταΓάμμα")] // Non-ascii letters
    [InlineData("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
    [InlineData("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
    [InlineData("person", "Person")]
    [InlineData("iPhone", "iPhone")]
    [InlineData("IPhone", "IPhone")]
    [InlineData("i Phone", "I Phone")]
    [InlineData("i  Phone", "I  Phone")]
    [InlineData(" IPhone", " IPhone")]
    [InlineData(" IPhone ", " IPhone ")]
    [InlineData("isCIA", "IsCIA")]
    [InlineData("vmQ", "VmQ")]
    [InlineData("xml2Json", "Xml2Json")]
    [InlineData("snAkEcAsE", "SnAkEcAsE")]
    [InlineData("snA__kEcAsE", "SnA__kEcAsE")]
    [InlineData("snA__ kEcAsE", "SnA__ kEcAsE")]
    [InlineData("already_snake_case_ ", "already_snake_case_ ")]
    [InlineData("isJSONProperty", "IsJSONProperty")]
    [InlineData("SHOUTING_CASE", "SHOUTING_CASE")]
    [InlineData("9999-12-31T23:59:59.9999999Z", "9999-12-31T23:59:59.9999999Z")]
    [InlineData("hi!! This is text. Time to test.", "Hi!! This is text. Time to test.")]
    [InlineData("BUILDING", "BUILDING")]
    [InlineData("BUILDING Property", "BUILDING Property")]
    [InlineData("building Property", "Building Property")]
    [InlineData("BUILDING PROPERTY", "BUILDING PROPERTY")]
    public static void ToCamelCaseTest(string expectedResult, string name)
    {
        var converted = name.ToCamelCase();
        Assert.Equal(expectedResult, converted);
    }

    [Fact]
    public static void CamelCaseNullNameReturnsNull()
    {
        const string name = null;
        Assert.Null(name.ToCamelCase());
    }
}
