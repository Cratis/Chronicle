// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from './Command';
import { CommandResult } from './CommandResult';
import { ICommandResult } from './ICommandResult';


export class CommandResults implements ICommandResult {
    constructor(private readonly _commandResultsPerCommand: Map<Command, CommandResult>) {
    }

    /** @inheritdoc */
    get isSuccess(): boolean {
        for (const result of this._commandResultsPerCommand.values()) {
            if (!result.isSuccess) {
                return false;
            }
        }
        return true;
    }
}
