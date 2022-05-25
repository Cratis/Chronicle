// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../Command';
import Handlebars from 'handlebars';
import { CommandValidator } from '../CommandValidator';

export interface ISomeCommand {
    someProperty: string;
}

export class SomeCommand extends Command<ISomeCommand> implements ISomeCommand {
    validation!: CommandValidator;
    route = '';
    routeTemplate!: Handlebars.TemplateDelegate;

    get requestArguments(): string[] {
        return [];
    }
    get properties(): string[] {
        return [
            'someProperty'
        ];
    }

    someProperty!: string;
}

