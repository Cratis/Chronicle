// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// The outcome of looking for a database without creating or binding anything.
/// </summary>
/// <remarks>
/// A negative probe is never proof of absence on its own: <see cref="Absent"/> requires positively established
/// catalog visibility, and everything that cannot be answered is <see cref="Unknown"/>.
/// </remarks>
internal abstract record DatabaseLocation
{
    /// <summary>
    /// Decides the outcome from the facts a probe gathered.
    /// </summary>
    /// <param name="boundConnectionString">The connection string of the permanent binding, if one exists.</param>
    /// <param name="seenConnectionString">The connection string of a database the catalog showed, if one was seen.</param>
    /// <param name="visibilityEstablished">Whether the probing identity is positively shown to see every database; null when not asked.</param>
    /// <param name="providerSupported">Whether the provider has a catalog to probe.</param>
    /// <param name="error">The exception thrown while gathering facts, if any.</param>
    /// <param name="stage">The stage that was running, named in an Unknown outcome.</param>
    /// <returns>The outcome.</returns>
    internal static DatabaseLocation Decide(string? boundConnectionString, string? seenConnectionString, bool? visibilityEstablished, bool providerSupported, Exception? error, string stage = "probe")
    {
        if (error is not null)
        {
            return new Unknown(stage, error.GetType().Name);
        }

        if (boundConnectionString is not null)
        {
            return new Found(boundConnectionString);
        }

        if (!providerSupported)
        {
            return new Unknown("probe", "unsupported provider");
        }

        if (seenConnectionString is not null)
        {
            return new Found(seenConnectionString);
        }

        return visibilityEstablished == true ? new Absent() : new Unknown("visibility", "catalog visibility not established");
    }

    /// <summary>
    /// The database exists at a connection string.
    /// </summary>
    /// <param name="ConnectionString">The connection string.</param>
    internal sealed record Found(string ConnectionString) : DatabaseLocation;

    /// <summary>
    /// The database is positively known not to exist.
    /// </summary>
    internal sealed record Absent : DatabaseLocation;

    /// <summary>
    /// Whether the database exists could not be established.
    /// </summary>
    /// <param name="Stage">The stage that could not answer.</param>
    /// <param name="Reason">A fixed reason or an exception type name, never a message or a connection string.</param>
    internal sealed record Unknown(string Stage, string Reason) : DatabaseLocation;
}
