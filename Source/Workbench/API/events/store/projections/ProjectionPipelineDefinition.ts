/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { ProjectionSinkDefinition } from './ProjectionSinkDefinition';

export class ProjectionPipelineDefinition {

    @field(String)
    projectionId!: string;

    @field(ProjectionSinkDefinition, true)
    sinks!: ProjectionSinkDefinition[];
}
