/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { ProjectionDefinition } from './ProjectionDefinition';
import { ProjectionPipelineDefinition } from './ProjectionPipelineDefinition';

export class ProjectionRegistration {

    @field(ProjectionDefinition)
    projection!: ProjectionDefinition;

    @field(ProjectionPipelineDefinition)
    pipeline!: ProjectionPipelineDefinition;
}
