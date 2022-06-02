// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Represents a validation error with a message for one or more members.
 */
export class ValidationError {
    constructor(readonly message: string, readonly memberNames: string[]) {

    }
}
