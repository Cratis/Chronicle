// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';
import { IDetailsComponentProps } from 'Components';
import { SchemaEditor, JsonSchema } from 'Components';
import { ReadModelDefinition, ReadModelSource, UpdateReadModelDefinition } from 'Api/ReadModelTypes';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';

export const ReadModelDetails = (props: IDetailsComponentProps<ReadModelDefinition>) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [schema, setSchema] = useState<JsonSchema>(() => JSON.parse(props.item.schema));
    const [updateReadModelDefinition] = UpdateReadModelDefinition.use();

    // Reset schema when read model changes
    useEffect(() => {
        setSchema(JSON.parse(props.item.schema));
    }, [props.item.identifier, props.item.schema]);

    const handleSave = async () => {
        updateReadModelDefinition.eventStore = params.eventStore!;
        updateReadModelDefinition.identifier = props.item.identifier;
        updateReadModelDefinition.containerName = props.item.containerName;
        updateReadModelDefinition.generation = props.item.generation;
        updateReadModelDefinition.schema = JSON.stringify(schema, null, 2);
        updateReadModelDefinition.indexes = Array.from(props.item.indexes);
        await updateReadModelDefinition.execute();
    };

    const handleSchemaChange = (newSchema: JsonSchema) => {
        setSchema(newSchema);
    };

    const canEdit = props.item.source !== ReadModelSource.code;
    const canEditReason = !canEdit ? strings.eventStore.general.readModels.cannotEditReason : undefined;

    return (
        <div className="read-model-details" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <SchemaEditor
                schema={schema}
                eventTypeName={props.item.containerName}
                canEdit={canEdit}
                canNotEditReason={canEditReason}
                onChange={handleSchemaChange}
                onSave={handleSave}
            />
        </div>
    );
};
