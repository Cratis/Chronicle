// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyIdentifierMigration;

[Collection(SqlServerCollection.Name)]
public class when_upgrading_on_sql_server(SqlServerFixture fixture) : for_EncryptionKeyStorage.given.a_key_store(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    List<string> _keyCollations;
    List<string> _erasureCollations;
    List<int> _keyLengths;
    List<int> _erasureLengths;
    int _keyRows;
    int _erasureRows;
    List<string> _keys;
    List<string> _reviews;
    List<string> _legacyCollations;
    string _expectedLegacyCollation;

    async Task Because()
    {
        _expectedLegacyCollation = (await _harness.Query<string>($"SELECT CAST(DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS nvarchar(128)) AS [Value]"))[0];
        await _harness.UpgradeFromEarlierLayout(
            "INSERT INTO [EncryptionKeys] ([Identifier], [Revision], [PublicKey], [PrivateKey]) VALUES (N'Alice', 1, 0x01, 0x11), (N'alice', 2, 0x02, 0x12), (N'bob ', 1, 0x03, 0x13), (N'carol' + NCHAR(1), 1, 0x04, 0x14), (N'dave', 3, 0x05, 0x15)",
            "INSERT INTO [EncryptionKeyErasures] ([Identifier], [ErasedThrough], [ErasedKeyFingerprints], [NewKeyAllowed]) VALUES (N'Dave', 1, N'', 0)");
        _keyCollations = await _harness.Query<string>($"SELECT collation_name AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'EncryptionKeys') AND name = 'Identifier'");
        _erasureCollations = await _harness.Query<string>($"SELECT collation_name AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'EncryptionKeyErasures') AND name = 'Identifier'");
        _keyLengths = await _harness.Query<int>($"SELECT CAST(max_length AS int) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'EncryptionKeys') AND name = 'Identifier'");
        _erasureLengths = await _harness.Query<int>($"SELECT CAST(max_length AS int) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'EncryptionKeyErasures') AND name = 'Identifier'");
        _keyRows = (await _harness.Query<int>($"SELECT COUNT(*) AS [Value] FROM [EncryptionKeys]"))[0];
        _erasureRows = (await _harness.Query<int>($"SELECT COUNT(*) AS [Value] FROM [EncryptionKeyErasures]"))[0];
        _keys = await _harness.Query<string>($"SELECT CONCAT(CONVERT(nvarchar(max), CAST([Identifier] AS varbinary(max)), 2), '/', [Revision], '/', CONVERT(varchar(20), [PublicKey], 2), '/', CONVERT(varchar(20), [PrivateKey], 2)) AS [Value] FROM [EncryptionKeys] ORDER BY [Revision], [Identifier]");
        _reviews = await _harness.Query<string>($"SELECT CONCAT(CONVERT(nvarchar(max), CAST([Identifier] AS varbinary(max)), 2), '/', CONVERT(nvarchar(max), CAST([LegacyIdentifier] AS varbinary(max)), 2)) AS [Value] FROM [EncryptionKeyIdentifierReviews]");
        _legacyCollations = await _harness.Query<string>($"SELECT collation_name AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'EncryptionKeyIdentifierReviews') AND name = 'LegacyIdentifier'");
    }

    static string Hex(string value) => Convert.ToHexString(System.Text.Encoding.Unicode.GetBytes(value));

    [Fact] void should_compare_key_identifiers_exactly() => _keyCollations.ShouldEqual(["Latin1_General_100_BIN2"]);
    [Fact] void should_compare_erasure_identifiers_exactly() => _erasureCollations.ShouldEqual(["Latin1_General_100_BIN2"]);
    [Fact] void should_widen_the_key_identifier_to_256_characters() => _keyLengths.ShouldEqual([512]);
    [Fact] void should_widen_the_erasure_identifier_to_256_characters() => _erasureLengths.ShouldEqual([512]);
    [Fact] void should_keep_every_key_row() => _keyRows.ShouldEqual(5);
    [Fact] void should_keep_every_erasure_row() => _erasureRows.ShouldEqual(1);

    [Fact]
    void should_keep_the_key_material_and_terminate_trailing_spaces() =>
        _keys.ShouldEqual([
            $"{Hex("Alice")}/1/01/11",
            $"{Hex("bob \u0001")}/1/03/13",
            $"{Hex("carol\u0001\u0001")}/1/04/14",
            $"{Hex("alice")}/2/02/12",
            $"{Hex("dave")}/3/05/15"]);

    [Fact]
    void should_record_every_stored_spelling_for_review() =>
        _reviews.Order().ToArray().ShouldEqual(new[]
        {
            $"{Hex("Alice")}/{Hex("Alice")}",
            $"{Hex("alice")}/{Hex("alice")}",
            $"{Hex("bob \u0001")}/{Hex("bob ")}",
            $"{Hex("carol\u0001\u0001")}/{Hex("carol\u0001")}",
            $"{Hex("dave")}/{Hex("dave")}",
            $"{Hex("Dave")}/{Hex("Dave")}"
        }.Order().ToArray());

    [Fact] void should_record_the_collation_the_columns_had_before() => _legacyCollations.ShouldEqual([_expectedLegacyCollation]);
}
