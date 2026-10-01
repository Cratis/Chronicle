// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for alerts raised by the kernel.
/// </summary>
public class Alerts
{
    /// <summary>
    /// Gets whether alerts are raised at all. When false, nothing is raised, whatever the individual conditions say.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the settings per condition, keyed by the hyphenated name of the condition, for example
    /// <c language="csharp">partition-failing</c>, <c language="csharp">partition-retries-exhausted</c> and <c language="csharp">observer-quarantined</c>.
    /// </summary>
    /// <remarks>
    /// A condition without an entry keeps its built-in defaults, and so does a setting an entry leaves out. The keys
    /// are not case sensitive. Kinds unknown to this kernel version are ignored, with a warning on first evaluation.
    /// Configuration, including an environment variable such as
    /// <c language="csharp">Cratis__Chronicle__Alerts__Conditions__partition-failing__RaiseAfter</c>, binds into this dictionary.
    /// </remarks>
    public IDictionary<string, AlertConditionOptions> Conditions { get; init; } =
        new Dictionary<string, AlertConditionOptions>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the observer identifiers to leave out of alerting, for known-noisy or development observers. An entry is
    /// either an identifier or a glob pattern where <c language="csharp">*</c> matches any number of characters and <c language="csharp">?</c> matches
    /// one.
    /// </summary>
    /// <remarks>
    /// The kernel's own alert observers are always left out and need no entry here.
    /// </remarks>
    public IEnumerable<string> ExcludeObservers { get; init; } = [];
}
