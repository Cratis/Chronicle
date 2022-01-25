// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand } from './ICommand';
import { CommandResult } from "./CommandResult";

/**
 * Represents an implementation of {@link ICommand} that works with HTTP fetch.
 */
export abstract class Command implements ICommand {
    abstract readonly route: string;

    /** @inheritdoc */
    async execute(): Promise<CommandResult> {
        const response = await fetch(this.route, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(this)
        });

        return new CommandResult(response);
    }
}
