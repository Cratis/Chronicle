// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Thrown before a metadata request with more than 500 locators is sent.
/// </summary>
public class TooManyEventLocators() : Exception("At most 500 event locators can be read at once.");
