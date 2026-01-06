import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { ShoppingCart, Eye } from 'lucide-react';
import { getProducts, updateBasket, getBasket } from '../api';

const CUSTOMER_ID = 'test-user';

export default function ProductList({ onBasketChange }) {
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        getProducts()
            .then(data => {
                setProducts(Array.isArray(data) ? data : []);
                setLoading(false);
            })
            .catch(err => {
                console.error('Error fetching products', err);
                setLoading(false);
            });
    }, []);

    const addToCart = async (product) => {
        try {
            const currentBasket = await getBasket(CUSTOMER_ID);
            const existingItem = currentBasket.items.find(i => i.productId === product.id);

            if (existingItem) {
                existingItem.quantity += 1;
            } else {
                currentBasket.items.push({
                    productId: product.id,
                    productName: product.name,
                    unitPrice: product.price,
                    quantity: 1
                });
            }

            await updateBasket(currentBasket);
            onBasketChange();
        } catch (error) {
            alert('Failed to add to cart: ' + error.message);
        }
    };

    if (loading) return <div className="container">Loading products...</div>;

    return (
        <div className="container">
            <header style={{ marginBottom: '2rem' }}>
                <h2>Discover Our Products</h2>
                <p style={{ color: 'var(--text-muted)' }}>Quality electronics and accessories for your needs.</p>
            </header>

            <div className="grid">
                {products.map(product => (
                    <div key={product.id} className="glass-card">
                        <img
                            src={product.imageUri || 'https://placehold.co/600x400?text=Product'}
                            alt={product.name}
                            style={{ width: '100%', borderRadius: '0.5rem', marginBottom: '1rem' }}
                        />
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start' }}>
                            <div>
                                <h3 style={{ fontSize: '1.25rem' }}>{product.name}</h3>
                                <span style={{ fontSize: '0.8rem', color: 'var(--primary)' }}>{product.categoryName}</span>
                            </div>
                            <span style={{ fontWeight: 'bold', fontSize: '1.2rem', color: 'var(--accent)' }}>${product.price}</span>
                        </div>
                        <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', margin: '1rem 0' }}>
                            {product.description && product.description.length > 80
                                ? product.description.substring(0, 80) + '...'
                                : (product.description || '')}
                        </p>
                        <div style={{ display: 'flex', gap: '0.5rem', marginTop: 'auto' }}>
                            <button onClick={() => addToCart(product)} className="btn btn-primary" style={{ flex: 1 }}>
                                <ShoppingCart size={18} /> Add
                            </button>
                            <Link to={`/product/${product.id}`} className="btn btn-outline" title="View details">
                                <Eye size={18} />
                            </Link>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}
