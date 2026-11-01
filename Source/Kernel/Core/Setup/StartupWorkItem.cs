// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting;

/// <summary>
/// Identifies one piece of recoverable startup work, for the single in-flight guarantee and for operator-facing logs.
/// </summary>
/// <param name="Kind">What the work is, for example ActivateObserver or RehydrateJobs.</param>
/// <param name="Key">The addressable identity of the work, built from event store, namespace and observer names only. It never carries event content.</param>
internal record StartupWorkItem(string Kind, string Key);
