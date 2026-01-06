// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { Dialog } from 'primereact/dialog';
import { TimeMachine, type Version } from '@cratis/components/TimeMachine';
import { useState, useEffect } from 'react';
import { AllSnapshotsForReadModel } from 'Api/ReadModels/AllSnapshotsForReadModel';
import { EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { ReadModelDefinition } from 'Api/ReadModelTypes/ReadModelDefinition';
import { ReadModelContent } from 'Features/EventStore/Namespaces/ReadModels/ReadModelContent';

export interface TimeMachineDialogProps {
    currentReadModel: string;
    currentReadModelKey: string;
    readModelDefinition: ReadModelDefinition;
}

export const TimeMachineDialog = ({ currentReadModel, currentReadModelKey, readModelDefinition }: TimeMachineDialogProps) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [versions, setVersions] = useState<Version[]>([]);
    const [currentVersion, setCurrentVersion] = useState(0);

    const [snapshots] = AllSnapshotsForReadModel.use({
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        readModel: currentReadModel,
        readModelKey: currentReadModelKey
    });

    const { closeDialog } = useDialogContext();

    // Convert snapshots to versions when data is loaded
    useEffect(() => {
        if (snapshots.data && snapshots.data.length > 0) {
            const convertedVersions: Version[] = snapshots.data.map((snapshot, index) => {
                const timestamp = new Date(snapshot.occurred);

                // Map backend events to component event format
                const mappedEvents = snapshot.events.map(event => ({
                    sequenceNumber: event.sequenceNumber,
                    type: event.type,
                    occurred: new Date(event.occurred),
                    content: event.content || {},
                }));

                return {
                    id: `snapshot-${index}`,
                    timestamp,
                    label: `Order @ ${snapshot.occurred.toLocaleString()}`,
                    content: <ReadModelContent readModel={snapshot.instance} timestamp={timestamp} readModelDefinition={readModelDefinition} />,
                    events: mappedEvents,
                };
            });
            setVersions(convertedVersions);
        }
    }, [snapshots.data]);

    const handleVersionChange = (index: number) => {
        setCurrentVersion(index);
    };

    return (
        <Dialog
            header="Time Machine"
            visible={true}
            onHide={() => closeDialog(DialogResult.Cancelled)}
            style={{ width: '80vw', height: '80vh' }}
            modal
            dismissableMask
            draggable={false}
            resizable={false}
        >
            <div style={{ height: 'calc(80vh - 100px)', display: 'flex', flexDirection: 'column' }}>
                {versions.length > 0 ? (
                    <TimeMachine
                        versions={versions}
                        currentVersionIndex={currentVersion}
                        onVersionChange={handleVersionChange}
                    />
                ) : (
                    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
                        <p>Loading snapshots...</p>
                    </div>
                )}
            </div>
        </Dialog>
    );
};

