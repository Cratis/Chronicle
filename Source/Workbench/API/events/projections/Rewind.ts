/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command } from '@aksio/cratis-applications-frontend/commands';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/projections/{{projectionId}}/rewind');

export class Rewind extends Command {
    readonly route: string = '/api/events/projections/{{projectionId}}/rewind';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;

    get requestArguments(): string[] {
        return [
            'projectionId',
        ];
    }

    projectionId!: string;
}
