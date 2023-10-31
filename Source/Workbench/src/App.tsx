import { useColorScheme } from './Utils/useColorScheme';
import { Home } from './Components/Pages/Home';
import { Layout } from './Layout/Layout';

function App() {
    useColorScheme();
    return (
        <>
            <Layout>
            <Home />
            </Layout>
        </>
    );
}

export default App;
