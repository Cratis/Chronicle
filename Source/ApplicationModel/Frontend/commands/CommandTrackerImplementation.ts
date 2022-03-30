// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { ICommand } from './ICommand';
import { CommandResults } from './CommandResults';
import { ICommandTracker } from './ICommandTracker';


/**
 * Represents an implementation of {@link ICommandTracker}.
 */
export class CommandTrackerImplementation implements ICommandTracker {
    private _commands: ICommand[] = [];
    private _hasChanges = false;

    constructor(private readonly _setHasChanges: React.Dispatch<React.SetStateAction<boolean>>) {
    }

    /** @inheritdoc */
    get hasChanges(): boolean {
        return this._hasChanges;
    }

    set hasChanges(value: boolean) {
        this._hasChanges = value;
    }

    /** @inheritdoc */
    addCommand(command: ICommand): void {
        if (this._commands.some(_ => _ == command)) {
            return;
        }

        this._commands.push(command);
        this.evaluateHasChanges();
        command.onPropertyChanged(this.evaluateHasChanges, this);
    }

    /** @inheritdoc */
    async execute(): Promise<CommandResults> {
        const commandsToCommandResult = new Map();

        for (const command of this._commands.filter(_ => _.hasChanges === true)) {
            const commandResult = await command.execute();
            commandsToCommandResult.set(command, commandResult);
        }
        this.evaluateHasChanges();
        return new CommandResults(commandsToCommandResult);
    }

    /** @inheritdoc */
    revertChanges() {
        this._commands.forEach(command => command.revertChanges());
        this.evaluateHasChanges();
    }

    private evaluateHasChanges() {
        let hasCommandChanges = false;
        this._commands.forEach(command => {
            if (command.hasChanges) {
                hasCommandChanges = true;
            }
        });

        this._hasChanges = hasCommandChanges;
        this._setHasChanges(hasCommandChanges);
    }
}
