// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Validator } from '../validation/Validator';

export type CommandPropertyValidators = { [key: string]: Validator; };

/**
 * Represents the command validator
 */
export abstract class CommandValidator {
    abstract readonly properties: CommandPropertyValidators;

    get isValid() {
        return true;
    }
}
