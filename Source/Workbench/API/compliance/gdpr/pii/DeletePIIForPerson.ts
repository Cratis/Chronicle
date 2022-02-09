/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command } from '@aksio/cratis-applications-frontend/commands';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/pii/delete');

export class DeletePIIForPerson extends Command {
    readonly route: string = '/api/compliance/gdpr/pii/delete';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;

    get requestArguments(): string[] {
        return [
        ];
    }

    personId!: string;
}
