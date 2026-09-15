// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { Dialog } from '@cratis/components/Dialogs';
import { ObjectContentEditor } from '@cratis/components/ObjectContentEditor';
import { ProgressSpinner } from '@cratis/components/Display';
import type { JsonSchema } from '@cratis/components/types';
import { useParams } from 'react-router-dom';
import { AllSnapshotsForReadModel } from 'Features/ReadModelExplorer';
import { ReadModelDefinition } from 'Features/ReadModelDefinitions';
import { EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { EventBubbles } from './EventBubbles';
import { ReadModelSnapshotGrouping } from './ReadModelSnapshotGrouping';
import { flattenSnapshots } from './flattenSnapshots';
import './TimeScrubberDialog.css';

/**
 * Props for {@link TimeScrubberDialog}.
 */
export interface TimeScrubberDialogProps {
    /** The read model the instance belongs to. */
    readModel: ReadModelDefinition;
    /** The key of the instance to scrub through. */
    readModelKey: string;
}

/**
 * Scrubs a read model instance through its own history.
 *
 * It reads the same snapshots the Time Machine does and renders them with the same renderer, so both
 * dialogs describe an instance from one source and a value looks the same wherever it is read. It
 * asks for them per event rather than per correlation, though: the Time Machine shows what each
 * action did, while scrubbing moves one event at a time, so every step moves the read model.
 * @param props The {@link TimeScrubberDialogProps}.
 * @returns The rendered dialog.
 */
export const TimeScrubberDialog = ({ readModel, readModelKey }: TimeScrubberDialogProps) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const { closeDialog } = useDialogContext();
    const [position, setPosition] = useState(0);

    const [snapshots] = AllSnapshotsForReadModel.use({
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        readModel: readModel.identifier,
        readModelKey,
        grouping: ReadModelSnapshotGrouping.Event
    });

    const steps = useMemo(() => flattenSnapshots(snapshots.data ?? []), [snapshots.data]);
    // The renderer needs a schema to lay values out by; an instance whose read model has none is
    // still worth showing, so it falls back to an empty one rather than refusing to render.
    const schema = useMemo<JsonSchema>(
        () => (readModel.schema ? JSON.parse(readModel.schema) as JsonSchema : {} as JsonSchema),
        [readModel.schema]);

    // A timeline that shrinks - a different instance, a reload - must not leave the scrubber past
    // its end, which would render nothing at all.
    const current = Math.min(position, Math.max(0, steps.length - 1));
    const step = steps[current];

    return (
        <Dialog
            title={strings.components.timeScrubber.title}
            visible={true}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
            width='80vw'
            style={{ height: '80vh' }}
            buttons={null}
            dismissable>
            <div className='time-scrubber'>
                {snapshots.isPerforming && steps.length === 0 && (
                    <div className='time-scrubber__message'>
                        <ProgressSpinner aria-label={strings.general.loading} />
                    </div>
                )}

                {!snapshots.isPerforming && steps.length === 0 && (
                    <div className='time-scrubber__message'>
                        <p>{strings.components.timeScrubber.empty}</p>
                    </div>
                )}

                {steps.length > 0 && step && (
                    <>
                        <div className='time-scrubber__content'>
                            <ObjectContentEditor
                                object={step.instance}
                                timestamp={new Date(step.occurred)}
                                schema={schema} />
                        </div>

                        <EventBubbles
                            steps={steps}
                            current={current}
                            onScrub={setPosition} />
                    </>
                )}
            </div>
        </Dialog>
    );
};
