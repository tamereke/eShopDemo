import { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom';
import { ShoppingCart, LayoutDashboard, Package, Home, ChevronRight } from 'lucide-react';
import ProductList from './pages/ProductList';
import ProductDetail from './pages/ProductDetail';
import Basket from './pages/Basket';
import Admin from './pages/Admin';
import Orders from './pages/Orders';
import { getBasket } from './api';
import Terminal from './Terminal';

const CUSTOMER_ID = 'test-user'; // Hardcoded for simplicity in demo

function App() {
  const [basketCount, setBasketCount] = useState(0);

  const refreshBasket = async () => {
    try {
      const data = await getBasket(CUSTOMER_ID);
      console.log('App: Basket data received', data);
      if (data && data.items) {
        const total = data.items.reduce((acc, item) => acc + item.quantity, 0);
        setBasketCount(total);
      }
    } catch (error) {
      console.error('App: Basket fetch failed', error);
    }
  };

  useEffect(() => {
    refreshBasket();
  }, []);

  return (
    <Router>
      <div className="app">
        <nav className="navbar">
          <div className="logo" style={{ fontSize: '1.5rem', fontWeight: '800', color: 'var(--primary)' }}>
            eShop<span style={{ color: 'var(--accent)' }}>Demo</span>
          </div>
          <div className="nav-links">
            <Link to="/"><Home size={18} /> Home</Link>
            <Link to="/orders"><Package size={18} /> Orders</Link>
            <Link to="/basket" className="basket-link">
              <ShoppingCart size={18} /> Basket
              {basketCount > 0 && <span className="badge">{basketCount}</span>}
            </Link>
            <Link to="/admin"><LayoutDashboard size={18} /> Admin</Link>
          </div>
        </nav>

        <main>
          <Routes>
            <Route path="/" element={<ProductList onBasketChange={refreshBasket} />} />
            <Route path="/product/:id" element={<ProductDetail onBasketChange={refreshBasket} />} />
            <Route path="/basket" element={<Basket customerId={CUSTOMER_ID} onOrderCreated={refreshBasket} />} />
            <Route path="/orders" element={<Orders />} />
            <Route path="/admin" element={<Admin />} />
          </Routes>
        </main>
        <Terminal />
      </div>
    </Router>
  );
}

export default App;
