// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';
import { IDetailsComponentProps } from 'Components';
import { SchemaEditor, JSONSchemaType } from 'Components';
import { EventTypeRegistration, EventTypeSource } from 'Api/Events';
import { Register } from 'Api/Events';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';

export const TypeDetails = (props: IDetailsComponentProps<EventTypeRegistration>) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [schema, setSchema] = useState<JSONSchemaType>(() => JSON.parse(props.item.schema));
    const [register] = Register.use();

    // Reset schema when event type changes
    useEffect(() => {
        setSchema(JSON.parse(props.item.schema));
    }, [props.item.type.id, props.item.schema]);

    const handleSave = async () => {
        register.eventStore = params.eventStore!;
        register.types = [{
            type: props.item.type,
            owner: props.item.owner,
            source: EventTypeSource.user,
            schema: JSON.stringify(schema, null, 2)
        }];

        await register.execute();
    };

    const handleSchemaChange = (newSchema: JSONSchemaType) => {
        setSchema(newSchema);
    };

    const canEdit = props.item.source !== EventTypeSource.code;
    const canEditReason = !canEdit ? strings.eventStore.general.types.cannotEditReason : undefined;

    return (
        <div className="type-details" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <SchemaEditor
                schema={schema}
                eventTypeName={props.item.type.id}
                canEdit={canEdit}
                canNotEditReason={canEditReason}
                onChange={handleSchemaChange}
                onSave={handleSave}
            />
        </div>
    );
};
