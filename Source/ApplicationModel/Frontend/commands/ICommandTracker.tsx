// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand } from './ICommand';
import { CommandResults } from './CommandResults';

/**
 * Defines the system for tracking commands.
 */
export interface ICommandTracker {
    /**
     * Gets whether or not there are any changes in the context.
     */
    readonly hasChanges: boolean;

    /**
     * Add a command for tracking in the context.
     * @param {ICommand} command Command to add.
     */
    addCommand(command: ICommand): void;

    /**
     * Execute all commands with changes.
     * @returns {Promise<CommandResults>} Command results per command that was executed.
     */
    execute(): Promise<CommandResults>;

    /**
     * Revert any changes for commands in context.
     */
    revertChanges(): void;
}
