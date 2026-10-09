import { Outlet } from 'react-router-dom';
import './App.css';
import NavMenu from './components/NavMenu';

function App() {
  return (
    <div>
      <NavMenu />
      <div className="container">
        <Outlet />
      </div>
      <footer className="container border-top mt-5 py-4 text-body-secondary small">
        Lootlark is a demo store built for learning. Payments run in test mode and no real money moves.
      </footer>
    </div>
  );
}

export default App;