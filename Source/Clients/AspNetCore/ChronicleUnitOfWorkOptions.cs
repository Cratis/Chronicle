// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Represents the settings for the ASP.NET Core request unit of work.
/// </summary>
public class ChronicleUnitOfWorkOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the middleware reports a failed commit as problem details (409 for
    /// conflicts, 400 for constraint violations, 500 otherwise) when the response has not started yet. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>Only applies to requests that did not claim completion through <c language="csharp">IUnitOfWorkCompletionOwnershipFeature</c>.</remarks>
    public bool ReportFailuresBeforeResponseStarts { get; set; }
}
