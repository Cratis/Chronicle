// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// The exception that is thrown when named tag criteria have an unset name or an invalid value set.
/// </summary>
public class InvalidNamedTagCriterion() : Exception("Named tag criteria require a nonblank name and a nonempty set of non-null values when values are supplied.");
