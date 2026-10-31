// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The exception that is thrown when artifact preparation or registration reenters its own unfinished local work.
/// </summary>
public class ArtifactPreparationReentered() : Exception("Artifact preparation or registration reentered its own unfinished local work; waiting would deadlock the owning preparation revision or registration lease.");
