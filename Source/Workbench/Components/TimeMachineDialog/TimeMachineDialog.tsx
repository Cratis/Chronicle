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
import { ObjectContentViewer } from 'Components/ObjectContentViewer';

export interface TimeMachineDialogProps {
    readModel: ReadModelDefinition;
    readModelKey: string;
}

export const TimeMachineDialog = ({ readModelKey, readModel }: TimeMachineDialogProps) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [versions, setVersions] = useState<Version[]>([]);
    const [currentVersion, setCurrentVersion] = useState(0);

    const [snapshots] = AllSnapshotsForReadModel.use({
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        readModel: readModel.identifier,
        readModelKey: readModelKey
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

                const schema = JSON.parse(readModel.schema);
                return {
                    id: `snapshot-${index}`,
                    timestamp,
                    label: `${readModel.identifier} @ ${snapshot.occurred.toLocaleString()}`,
                    content: <ObjectContentViewer object={snapshot.instance} timestamp={timestamp} schema={schema} />,
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


/*
at System.Collections.Generic.Dictionary\u00602.Add(TKey key, TValue value)\n
at System.Linq.Enumerable.SpanToDictionary[TSource,TKey](ReadOnlySpan\u00601 source, Func\u00602 keySelector, IEqualityComparer\u00601 comparer)\n
at System.Linq.Enumerable.ToDictionary[TSource,TKey](IEnumerable\u00601 source, Func\u00602 keySelector, IEqualityComparer\u00601 comparer)\n
at Cratis.Chronicle.Projections.DefinitionLanguage.ProjectionValidator..ctor(IEnumerable\u00601 readModelDefinitions, IEnumerable\u00601 eventTypeSchemas) in /Volumes/Code/Cratis/Chronicle/Source/Kernel/Projections/DefinitionLanguage/ProjectionValidator.cs:line 25\n
at Cratis.Chronicle.Projections.DefinitionLanguage.Compiler.Compile(Document document, ProjectionOwner owner, IEnumerable\u00601 readModelDefinitions, IEnumerable\u00601 eventTypeSchemas) in /Volumes/Code/Cratis/Chronicle/Source/Kernel/Projections/DefinitionLanguage/Compiler.cs:line 75\n   at Cratis.Chronicle.Projections.DefinitionLanguage.LanguageService.\u003C\u003Ec__DisplayClass4_0.\u003CCompile\u003Eb__2(Document document) in /Volumes/Code/Cratis/Chronicle/Source/Kernel/Projections/DefinitionLanguage/LanguageService.cs:line 43\n   at OneOf.OneOfBase\u00602.Match[TResult](Func\u00602 f0, Func\u00602 f1)\n   at Cratis.Chronicle.Projections.DefinitionLanguage.LanguageService.\u003C\u003Ec__DisplayClass4_0.\u003CCompile\u003Eb__0(IEnumerable\u00601 tokens) in /Volumes/Code/Cratis/Chronicle/Source/Kernel/Projections/DefinitionLanguage/LanguageService.cs:line 39\n   at OneOf.OneOfBase\u00602.Match[TResult](Func\u00602 f0, Func\u00602 f1)\n   at Cratis.Chronicle.Projections.DefinitionLanguage.LanguageService.Compile(String definition, ProjectionOwner owner, IEnumerable\u00601 readModelDefinitions, IEnumerable\u00601 eventTypeSchemas) in /Volumes/Code/Cratis/Chronicle/Source/Kernel/Projections/DefinitionLanguage/LanguageService.cs:line 33\n   at Cratis.Chronicle.Services.Projections.Projections.Preview(PreviewProjectionRequest request, CallContext context) in /Volumes/Code/Cratis/Chronicle/Source/Kernel/Services/Projections/Projections.cs:line 92\n   at Cratis.Chronicle.Api.Projections.PreviewProjection.Handle(IProjections projections)\n   at Cratis.Tasks.AwaitableHelpers.AwaitIfNeeded(Object maybeAwaitable)\n   at Cratis.Arc.Commands.ModelBound.ModelBoundCommandHandler.Handle(CommandContext commandContext)\n   at Cratis.Arc.Commands.CommandPipeline.Execute(Object command, IServiceProvider serviceProvider)",

    */
