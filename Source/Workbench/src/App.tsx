import ToggleableDemo from './refactorThis/ToggeableDemo';
import { useColorScheme } from './Utils/useColorScheme';
import BasicButton from './refactorThis/BasicButton';
import DialogDemo from './refactorThis/DialogDemo';
import BasicDemo from './refactorThis/BasicDemo';
import InputDemo from './refactorThis/InputDemo';
import { Layout } from './Layout/Layout';

function App() {
    useColorScheme();
    return (
        <Layout>
            <div className='grid grid-flow-col justify-stretch gap-4 m-3'>
                <BasicDemo />
                <BasicDemo />
                <BasicDemo />
            </div>

            <div className='grid grid-flow-col justify-stretch gap-4 m-3'>
                <DialogDemo />
                <DialogDemo />
                <DialogDemo />
            </div>
            <div className='grid grid-flow-col justify-stretch gap-4 m-3'>
                <ToggleableDemo />
                <ToggleableDemo />
                <ToggleableDemo />
            </div>
            <div className='grid grid-flow-col justify-stretch gap-4 m-3'>
                <InputDemo />
                <InputDemo />
                <InputDemo />
            </div>
            <div className='grid grid-flow-col justify-stretch gap-4 m-3'>
                <BasicButton />
                <BasicButton />
                <BasicButton />
            </div>
        </Layout>
    );
}

export default App;
