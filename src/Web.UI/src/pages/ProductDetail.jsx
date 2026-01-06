import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ShoppingCart, ArrowLeft } from 'lucide-react';
import { getProducts, getBasket, updateBasket } from '../api';

const CUSTOMER_ID = 'test-user';

export default function ProductDetail({ onBasketChange }) {
    const { id } = useParams();
    const navigate = useNavigate();
    const [product, setProduct] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // API has no getProductById in gateway yet, using the list search logic for now or listing all
        getProducts().then(data => {
            const found = data.find(p => p.id === id);
            setProduct(found);
            setLoading(false);
        });
    }, [id]);

    const addToCart = async () => {
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
            alert('Failed: ' + error.message);
        }
    };

    if (loading) return <div className="container">Loading...</div>;
    if (!product) return <div className="container">Product not found.</div>;

    return (
        <div className="container">
            <button onClick={() => navigate(-1)} className="btn btn-outline" style={{ marginBottom: '2rem' }}>
                <ArrowLeft size={18} /> Back
            </button>

            <div className="glass-card" style={{ display: 'grid', gridTemplateColumns: 'minmax(300px, 1fr) 1.5fr', gap: '3rem', cursor: 'default' }}>
                <img
                    src={product.imageUri || 'https://placehold.co/600x400?text=Product'}
                    alt={product.name}
                    style={{ width: '100%', borderRadius: '1rem' }}
                />
                <div>
                    <span style={{ color: 'var(--primary)', fontWeight: '600' }}>{product.categoryName}</span>
                    <h1 style={{ fontSize: '2.5rem', marginTop: '0.5rem' }}>{product.name}</h1>
                    <p style={{ fontSize: '1.5rem', color: 'var(--accent)', margin: '1rem 0', fontWeight: 'bold' }}>${product.price}</p>

                    <div style={{ height: '1px', background: 'var(--border)', margin: '1.5rem 0' }}></div>

                    <p style={{ color: 'var(--text-muted)', lineHeight: '1.8', fontSize: '1.1rem' }}>
                        {product.description || ''}
                    </p>

                    <div style={{ marginTop: '2rem' }}>
                        <button onClick={addToCart} className="btn btn-primary" style={{ padding: '1rem 2rem', fontSize: '1.1rem' }}>
                            <ShoppingCart size={20} /> Add to Selection
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}
