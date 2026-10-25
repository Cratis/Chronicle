// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicTableShape;

public class when_comparing_provider_store_types : Specification
{
    [Theory]
    [InlineData("character varying(200)", "varchar(200)")]
    [InlineData("character(20)", "char(20)")]
    [InlineData("timestamp with time zone", "timestamptz")]
    [InlineData("timestamp without time zone", "timestamp")]
    [InlineData("time without time zone", "time")]
    [InlineData("timestamp(3) without time zone", "timestamp(3)")]
    [InlineData("time(6) without time zone", "time(6)")]
    [InlineData("int4", "integer")]
    [InlineData("int8", "bigint")]
    [InlineData("int2", "smallint")]
    [InlineData("bool", "boolean")]
    [InlineData("float4", "real")]
    [InlineData("float8", "double precision")]
    [InlineData("INT", "integer")]
    [InlineData("DECIMAL(18, 2)", "numeric(18,2)")]
    [InlineData("NVARCHAR(MAX)", "nvarchar(max)")]
    [InlineData("NCHAR(10)", "nchar(10)")]
    public void should_accept_equivalent_provider_spellings(string expected, string actual) =>
        DynamicTableShape.Normalize(expected).ShouldEqual(DynamicTableShape.Normalize(actual));

    [Theory]
    [InlineData("varchar(100)", "varchar(200)")]
    [InlineData("nvarchar(max)", "nvarchar(200)")]
    [InlineData("nchar(10)", "nvarchar(10)")]
    [InlineData("decimal(18,2)", "numeric(19,2)")]
    [InlineData("decimal(18,2)", "numeric(18,3)")]
    [InlineData("integer", "bigint")]
    [InlineData("real", "double precision")]
    [InlineData("timestamp", "timestamptz")]
    [InlineData("datetime2(3)", "datetime2(7)")]
    [InlineData("timestamp(3)", "timestamp(6)")]
    public void should_preserve_meaningful_type_differences(string expected, string actual) =>
        (DynamicTableShape.Normalize(expected) == DynamicTableShape.Normalize(actual)).ShouldBeFalse();
}
