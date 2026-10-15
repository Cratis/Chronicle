// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Thrown when a metadata read exceeds the bounded locator limit.
/// </summary>
public class TooManyEventLocators() : Exception("At most 500 event locators can be read at once.");
