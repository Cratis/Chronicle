// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from './CommandResult';

/**
 * Defines the base of a command.
 */
export interface ICommand {
    /**
     * Gets the route information for the command.
     */
    readonly route: string;

    /**
     * Execute the {@link ICommand}.
     * @param [args] Optional arguments for the command route - depends on whether or not the command needs arguments.
     * @returns {CommandResult} for the execution.
     */
    execute(): Promise<CommandResult>;
}
