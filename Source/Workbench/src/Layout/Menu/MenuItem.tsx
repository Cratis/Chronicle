import { PanelMenu } from 'primereact/panelmenu';
import { MenuItem } from 'primereact/menuitem';
import {
    FaEye,
    FaVial,
    FaLock,
    FaPlus,
    FaEject,
    FaHubspot,
    FaCartPlus,
    FaDatabase,
    FaAlignLeft,
    FaTriangleExclamation,
} from 'react-icons/fa6';

export const Menuitem = () => {
    const items: MenuItem[] = [
        {
            label: 'New',
            icon: (
                <FaPlus
                    style={{
                        marginRight: 12,
                    }}
                />
            ),
        },
        {
            label: 'Arendal',
            icon: (
                <FaDatabase
                    style={{
                        marginRight: 12,
                    }}
                />
            ),
            items: [
                {
                    label: 'Sequences',
                    icon: (
                        <FaEject
                            style={{
                                marginRight: 12,
                            }}
                        />
                    ),
                },
                {
                    label: 'Observers',
                    icon: (
                        <FaEye
                            style={{
                                marginRight: 12,
                            }}
                        />
                    ),
                },
                {
                    label: 'Failed Partitions',
                    icon: (
                        <FaTriangleExclamation
                            style={{
                                marginRight: 12,
                            }}
                        />
                    ),
                },
            ],
        },
        {
            label: 'Event Store',
            icon: (
                <FaCartPlus
                    style={{
                        marginRight: 12,
                    }}
                />
            ),
            items: [
                {
                    label: 'Event Types',
                    icon: (
                        <FaAlignLeft
                            style={{
                                marginRight: 12,
                            }}
                        />
                    ),
                },
                {
                    label: 'Projections',
                    icon: (
                        <FaVial
                            style={{
                                marginRight: 12,
                            }}
                        />
                    ),
                },
                {
                    label: 'Sinks',
                    icon: (
                        <FaHubspot
                            style={{
                                marginRight: 12,
                            }}
                        />
                    ),
                },
                {
                    label: 'Compliance',
                    icon: (
                        <FaLock
                            style={{
                                marginRight: 12,
                            }}
                        />
                    ),
                },
            ],
        },
    ];
    return (
        <div className='card flex justify-content-center'>
            <PanelMenu multiple model={items} className='w-full md:w-25rem' />
        </div>
    );
};
