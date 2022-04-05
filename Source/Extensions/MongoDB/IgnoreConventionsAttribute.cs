// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Used to mark models for ignoring specific <see cref="ConventionPacks">convention packs</see>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class IgnoreConventionsAttribute : Attribute
{
    /// <summary>
    /// Gets convention packs to ignore.
    /// </summary>
    public string[] ConventionPacks { get; }

    /// <summary>
    /// Gets whether or not to ignore all convention packs.
    /// </summary>
    public bool IgnoreAll => ConventionPacks.Length == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreConventionsAttribute"/> class.
    /// </summary>
    /// <param name="conventionPacks">Any specific <see cref="ConventionPacks">convention packs</see> to ignore. If none are given, all convention packs will be ignored.</param>
    public IgnoreConventionsAttribute(params string[] conventionPacks)
    {
        ConventionPacks = conventionPacks;
    }
}
