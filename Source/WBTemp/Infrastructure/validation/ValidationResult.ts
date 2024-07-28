// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResultSeverity } from './ValidationResultSeverity';

/**
 * Represents a validation error with a message for one or more members.
 */
export class ValidationResult {
    constructor(readonly severity: ValidationResultSeverity, readonly message: string, readonly members: string[], readonly state: any) {
    }
}


