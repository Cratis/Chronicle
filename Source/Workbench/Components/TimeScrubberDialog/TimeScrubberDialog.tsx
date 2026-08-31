// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { Dialog } from '@cratis/components/Dialogs';
import { ObjectContentEditor } from '@cratis/components/ObjectContentEditor';
import { ProgressSpinner } from '@cratis/components/Display';
import type { Json, JsonSchema } from '@cratis/components/types';
import { useParams } from 'react-router-dom';
import { TimelineForReadModel } from 'Api/ReadModels';
import { ReadModelDefinition } from 'Api/ReadModelTypes/ReadModelDefinition';
import { EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { EventBubbles } from './EventBubbles';
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
 * The read model is shown with the same renderer the Time Machine uses, so a value looks the same
 * wherever it is read. Below it sits one bubble per event: hovering one says what happened, and
 * moving the scrubber shows the read model as it stood right after that event.
 * @param props The {@link TimeScrubberDialogProps}.
 * @returns The rendered dialog.
 */
export const TimeScrubberDialog = ({ readModel, readModelKey }: TimeScrubberDialogProps) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const { closeDialog } = useDialogContext();
    const [position, setPosition] = useState(0);

    const [timeline] = TimelineForReadModel.use({
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        readModel: readModel.identifier,
        readModelKey
    });

    const entries = useMemo(() => timeline.data ?? [], [timeline.data]);
    // The renderer needs a schema to lay values out by; an instance whose read model has none is
    // still worth showing, so it falls back to an empty one rather than refusing to render.
    const schema = useMemo<JsonSchema>(
        () => (readModel.schema ? JSON.parse(readModel.schema) as JsonSchema : {} as JsonSchema),
        [readModel.schema]);

    // A timeline that shrinks - a different instance, a reload - must not leave the scrubber past
    // its end, which would render nothing at all.
    const current = Math.min(position, Math.max(0, entries.length - 1));
    const entry = entries[current];

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
                {timeline.isPerforming && entries.length === 0 && (
                    <div className='time-scrubber__message'>
                        <ProgressSpinner aria-label={strings.general.loading} />
                    </div>
                )}

                {!timeline.isPerforming && entries.length === 0 && (
                    <div className='time-scrubber__message'>
                        <p>{strings.components.timeScrubber.empty}</p>
                    </div>
                )}

                {entries.length > 0 && entry && (
                    <>
                        <div className='time-scrubber__content'>
                            <ObjectContentEditor
                                object={entry.instance as Json}
                                timestamp={new Date(entry.event.occurred)}
                                schema={schema} />
                        </div>

                        <EventBubbles
                            entries={entries}
                            current={current}
                            onScrub={setPosition} />
                    </>
                )}
            </div>
        </Dialog>
    );
};
