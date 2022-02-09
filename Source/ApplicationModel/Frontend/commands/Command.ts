// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand } from './ICommand';
import { CommandResult } from "./CommandResult";

/**
 * Represents an implementation of {@link ICommand} that works with HTTP fetch.
 */
export abstract class Command implements ICommand {
    abstract readonly route: string;
    abstract readonly routeTemplate: Handlebars.TemplateDelegate;
    abstract get requestArguments(): string[];

    /** @inheritdoc */
    async execute(): Promise<CommandResult> {
        let actualRoute = this.route;
        if (this.requestArguments && this.requestArguments.length > 0) {
            actualRoute = this.routeTemplate(this);
        }

        const response = await fetch(actualRoute, {
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
