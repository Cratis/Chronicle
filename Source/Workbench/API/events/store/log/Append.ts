/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command } from '@aksio/cratis-applications-frontend/commands';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/log/{{eventSourceId}}/{{eventTypeId}}/{{eventGeneration}}');

export class Append extends Command {
    readonly route: string = '/api/events/store/log/{{eventSourceId}}/{{eventTypeId}}/{{eventGeneration}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;

    get requestArguments(): string[] {
        return [
            'eventSourceId',
            'eventTypeId',
            'eventGeneration',
        ];
    }

    eventSourceId!: string;
    eventTypeId!: string;
    eventGeneration!: number;
}
