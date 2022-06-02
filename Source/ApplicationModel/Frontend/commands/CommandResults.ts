// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@aksio/cratis-fundamentals';
import { ValidationError } from '../validation/ValidationError';
import { Command } from './Command';
import { CommandResult } from './CommandResult';
import { ICommandResult } from './ICommandResult';

type GetSpecificState = (commandResult: CommandResult) => boolean;


export class CommandResults implements ICommandResult {
    constructor(private readonly _commandResultsPerCommand: Map<Command, CommandResult>) {
    }

    /** @inheritdoc */
    readonly correlationId: Guid = Guid.empty;

    /** @inheritdoc */
    get isSuccess(): boolean {
        return this.isAnyFalse(_ => _.isSuccess);
    }

    /** @inheritdoc */
    get isAuthorized(): boolean {
        return this.isAnyFalse(_ => _.isAuthorized);
    }

    /** @inheritdoc */
    get isValid(): boolean {
        return this.isAnyFalse(_ => _.isValid);
    }

    /** @inheritdoc */
    get hasExceptions(): boolean {
        return this.isAnyTrue(_ => _.hasExceptions);
    }

    /** @inheritdoc */
    get validationErrors(): ValidationError[] {
        const errors: ValidationError[] = [];

        for (const result of this._commandResultsPerCommand.values()) {
            result.validationErrors.forEach(_ => errors.push(_));
        }

        return errors;
    }

    /** @inheritdoc */
    get exceptionMessages(): string[] {
        const messages: string[] = [];

        for (const result of this._commandResultsPerCommand.values()) {
            result.exceptionMessages.forEach(_ => messages.push(_));
        }

        return messages;
    }

    /** @inheritdoc */
    get exceptionStackTrace(): string {
        let stackTraces = '';

        for (const result of this._commandResultsPerCommand.values()) {
            stackTraces = stackTraces + '\n' + result.exceptionStackTrace;
        }

        return stackTraces;

    }


    isAnyFalse(callback: GetSpecificState) {
        for (const result of this._commandResultsPerCommand.values()) {
            if (!callback(result)) {
                return false;
            }
        }
        return true;
    }

    isAnyTrue(callback: GetSpecificState) {
        for (const result of this._commandResultsPerCommand.values()) {
            if (callback(result)) {
                return true;
            }
        }
        return false;
    }
}
