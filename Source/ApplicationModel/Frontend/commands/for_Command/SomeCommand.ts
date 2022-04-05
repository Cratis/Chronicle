// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../Command';
import Handlebars from 'handlebars';
import { CommandValidator } from '../CommandValidator';

export class SomeCommand extends Command {
    validation!: CommandValidator;
    route = '';
    routeTemplate!: Handlebars.TemplateDelegate<any>;

    get requestArguments(): string[] {
        throw new Error('Method not implemented.');
    }
    get properties(): string[] {
        return [
            'someProperty'
        ];
    }

    someProperty!: string;
}
