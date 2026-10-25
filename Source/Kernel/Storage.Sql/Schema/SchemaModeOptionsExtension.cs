// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Carries the schema policy into EF's internal service provider.
/// </summary>
/// <param name="mode">The schema policy.</param>
/// <remarks>
/// The policy is read from scoped context options, not singleton EF services, so modes share a service provider.
/// </remarks>
internal sealed class SchemaModeOptionsExtension(SchemaMode mode) : IDbContextOptionsExtension
{
    /// <inheritdoc/>
    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    /// <summary>
    /// Gets the schema policy.
    /// </summary>
    internal SchemaMode Mode { get; } = mode;

    /// <inheritdoc/>
    public void ApplyServices(IServiceCollection services)
    {
    }

    /// <inheritdoc/>
    public void Validate(IDbContextOptions options)
    {
    }

    sealed class ExtensionInfo(SchemaModeOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => $"SchemaMode={Mode} ";
        SchemaMode Mode => ((SchemaModeOptionsExtension)Extension).Mode;
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is ExtensionInfo;
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) => debugInfo["Chronicle:SchemaMode"] = "0";
    }
}
