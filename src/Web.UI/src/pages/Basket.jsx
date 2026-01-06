import { useState, useEffect } from 'react';
import { Trash2, ShoppingBag, CreditCard } from 'lucide-react';
import { getBasket, updateBasket, createOrder, deleteBasket } from '../api';

export default function Basket({ customerId, onOrderCreated }) {
    const [basket, setBasket] = useState(null);
    const [loading, setLoading] = useState(true);
    const [ordered, setOrdered] = useState(false);

    useEffect(() => {
        fetchBasket();
    }, [customerId]);

    const fetchBasket = () => {
        getBasket(customerId).then(data => {
            setBasket(data);
            setLoading(false);
        });
    };

    const removeItem = async (productId) => {
        const updated = { ...basket, items: basket.items.filter(i => i.productId !== productId) };
        await updateBasket(updated);
        setBasket(updated);
        onOrderCreated(); // Updates nav count
    };

    const checkout = async () => {
        try {
            if (basket.items.length === 0) return;

            const orderRequest = {
                customerId: customerId,
                items: basket.items.map(i => ({
                    productId: i.productId,
                    productName: i.productName,
                    quantity: i.quantity,
                    unitPrice: i.unitPrice
                }))
            };

            await createOrder(orderRequest);

            // Clear the basket backend
            await deleteBasket(customerId);

            setOrdered(true);
            onOrderCreated();
        } catch (error) {
            alert('Checkout failed: ' + error.message);
        }
    };

    if (loading) return <div className="container">Loading basket...</div>;
    if (ordered) return (
        <div className="container" style={{ textAlign: 'center', paddingTop: '5rem' }}>
            <div className="glass-card" style={{ maxWidth: '600px', margin: '0 auto' }}>
                <h2 style={{ color: 'var(--primary)' }}>Order Placed Successfully!</h2>
                <p style={{ margin: '1rem 0', color: 'var(--text-muted)' }}>Thank you for your purchase. Your order is being processed.</p>
                <button className="btn btn-primary" onClick={() => window.location.href = '/'}>Back to Shop</button>
            </div>
        </div>
    );

    if (!basket || basket.items.length === 0) return (
        <div className="container" style={{ textAlign: 'center', paddingTop: '5rem' }}>
            <ShoppingBag size={64} style={{ color: 'var(--text-muted)', marginBottom: '1rem' }} />
            <h2>Your basket is empty</h2>
            <p style={{ color: 'var(--text-muted)' }}>Looks like you haven't added anything yet.</p>
        </div>
    );

    return (
        <div className="container">
            <h2>Your Shopping Bag</h2>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 350px', gap: '2rem', marginTop: '2rem' }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                    {basket.items.map(item => (
                        <div key={item.productId} className="glass-card" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem' }}>
                            <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
                                <div style={{ width: '60px', height: '60px', background: 'rgba(255,255,255,0.05)', borderRadius: '0.5rem' }}></div>
                                <div>
                                    <h4 style={{ margin: 0 }}>{item.productName}</h4>
                                    <p style={{ margin: 0, fontSize: '0.85rem', color: 'var(--text-muted)' }}>Quantity: {item.quantity}</p>
                                </div>
                            </div>
                            <div style={{ display: 'flex', gap: '2rem', alignItems: 'center' }}>
                                <span style={{ fontWeight: '600' }}>${item.unitPrice * item.quantity}</span>
                                <button onClick={() => removeItem(item.productId)} style={{ background: 'transparent', border: 'none', color: '#ef4444', cursor: 'pointer' }}>
                                    <Trash2 size={20} />
                                </button>
                            </div>
                        </div>
                    ))}
                </div>

                <div className="glass-card" style={{ height: 'fit-content' }}>
                    <h3>Summary</h3>
                    <div style={{ margin: '1.5rem 0' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                            <span style={{ color: 'var(--text-muted)' }}>Subtotal</span>
                            <span>${basket.totalPrice}</span>
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                            <span style={{ color: 'var(--text-muted)' }}>Shipping</span>
                            <span style={{ color: '#4ade80' }}>Free</span>
                        </div>
                    </div>
                    <div style={{ height: '1px', background: 'var(--border)', margin: '1rem 0' }}></div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '1.25rem', fontWeight: '800', marginBottom: '2rem' }}>
                        <span>Total</span>
                        <span style={{ color: 'var(--accent)' }}>${basket.totalPrice}</span>
                    </div>
                    <button onClick={checkout} className="btn btn-primary" style={{ width: '100%', justifyContent: 'center' }}>
                        <CreditCard size={20} /> Secure Checkout
                    </button>
                </div>
            </div>
        </div>
    );
}
