// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllReadModelDefinitions } from 'Api/ReadModelTypes';
import { Page } from 'Components/Common/Page';
import { JsonSchema } from 'Components/JsonSchema';
import { ProjectionEditor } from 'Components/ProjectionEditor';
import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { EventStoreAndNamespaceParams } from 'Shared/EventStoreAndNamespaceParams';

export const Projections = () => {

    const [dslValue, setDslValue] = useState('');

    /*`Users
| key=UserRegistered.userId
| name=UserRegistered.name
| email=UserRegistered.email
| totalSpent+OrderCompleted.amount
| orderCount increment by OrderPlaced
| orderCount decrement by OrderCancelled
| lastLogin=$eventContext.occurred
| status="active" on UserRegistered
| orders=[
|    identified by orderId
|    key=OrderPlaced.orderId
|    total=OrderPlaced.total
| ]`);*/

    const params = useParams<EventStoreAndNamespaceParams>();

    const [readModels] = AllReadModelDefinitions.use({ eventStore: params.eventStore! });
    const readModelSchemas = readModels.data?.map(readModel => JSON.parse(readModel.schema) as JsonSchema);

    return (
        <Page title='Projections'>
            <div style={{ padding: '20px' }}>
                <ProjectionEditor
                    value={dslValue}
                    onChange={setDslValue}
                    readModelSchemas={readModelSchemas}
                    height="500px"
                    theme="vs-dark"
                />

                <div style={{ marginTop: '20px' }}>
                    <h3>Features Demonstrated:</h3>
                    <ul>
                        <li>✅ Syntax highlighting for keywords and operators</li>
                        <li>✅ Type-aware validation (arithmetic only on numeric properties)</li>
                        <li>✅ IntelliSense for properties, keywords, and event context</li>
                        <li>✅ Error markers for invalid syntax</li>
                        <li>✅ Support for nested children definitions</li>
                    </ul>

                    <h3>Try These:</h3>
                    <ul>
                        <li>Type <code>| name+</code> - you'll see an error because name is a string</li>
                        <li>Type <code>| total</code> and press space - you'll see suggestions</li>
                        <li>Type <code>| $eventContext.</code> - you'll see context property suggestions</li>
                        <li>Type <code>| orderCount increment by</code> - valid for numeric types</li>
                    </ul>
                </div>
            </div>
        </Page>
    );
};
