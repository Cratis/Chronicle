// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// The exception that is thrown when a named tag has an unset name or null value, or a context contains a null named tag.
/// </summary>
public class InvalidNamedTag() : Exception("Named tags require a nonblank name and a non-null value; named tag collections cannot contain null elements.");
