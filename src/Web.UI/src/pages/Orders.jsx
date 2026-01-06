import { useState, useEffect } from 'react';
import { Package, Clock, CheckCircle, ChevronDown, ChevronUp } from 'lucide-react';
import { getOrdersByCustomer } from '../api';

const CUSTOMER_ID = 'test-user';

export default function Orders() {
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [expandedOrder, setExpandedOrder] = useState(null);

    useEffect(() => {
        getOrdersByCustomer(CUSTOMER_ID)
            .then(data => {
                setOrders(data || []);
                setLoading(false);
            })
            .catch(err => {
                console.error('Failed to fetch orders', err);
                setLoading(false);
            });
    }, []);

    const toggleOrder = (orderId) => {
        setExpandedOrder(expandedOrder === orderId ? null : orderId);
    };

    const formatDate = (dateString) => {
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    if (loading) return <div className="container">Loading orders...</div>;

    return (
        <div className="container">
            <h2>My Orders</h2>

            {orders.length === 0 ? (
                <div className="glass-card" style={{ textAlign: 'center', marginTop: '2rem', padding: '3rem' }}>
                    <Package size={48} style={{ color: 'var(--text-muted)', marginBottom: '1rem' }} />
                    <h3>No orders found</h3>
                    <p style={{ color: 'var(--text-muted)' }}>You haven't placed any orders yet.</p>
                </div>
            ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem', marginTop: '2rem' }}>
                    {orders.map(order => (
                        <div key={order.id} className="glass-card" style={{ padding: '0' }}>
                            <div
                                onClick={() => toggleOrder(order.id)}
                                style={{
                                    padding: '1.5rem',
                                    display: 'flex',
                                    justifyContent: 'space-between',
                                    alignItems: 'center',
                                    cursor: 'pointer',
                                    borderBottom: expandedOrder === order.id ? '1px solid var(--border)' : 'none'
                                }}
                            >
                                <div style={{ display: 'flex', gap: '1.5rem', alignItems: 'center' }}>
                                    <div style={{
                                        background: 'rgba(56, 189, 248, 0.1)',
                                        color: 'var(--primary)',
                                        padding: '0.75rem',
                                        borderRadius: '0.5rem'
                                    }}>
                                        <Package size={24} />
                                    </div>
                                    <div>
                                        <h4 style={{ margin: 0 }}>Order #{(order.id || order.orderId || '').substring(0, 8)}</h4>
                                        <span style={{ fontSize: '0.9rem', color: 'var(--text-muted)' }}>
                                            <Clock size={14} style={{ display: 'inline', marginRight: '4px' }} />
                                            {formatDate(order.createdAt)}
                                        </span>
                                    </div>
                                </div>

                                <div style={{ display: 'flex', gap: '2rem', alignItems: 'center' }}>
                                    <div style={{ textAlign: 'right' }}>
                                        <div style={{ fontWeight: 'bold', fontSize: '1.1rem' }}>
                                            ${order.items.reduce((acc, item) => acc + (item.unitPrice * item.quantity), 0).toFixed(2)}
                                        </div>
                                        <div style={{
                                            fontSize: '0.85rem',
                                            color: '#4ade80',
                                            display: 'flex',
                                            alignItems: 'center',
                                            gap: '4px',
                                            justifyContent: 'flex-end'
                                        }}>
                                            <CheckCircle size={14} /> {order.status}
                                        </div>
                                    </div>
                                    {expandedOrder === order.id ? <ChevronUp size={20} /> : <ChevronDown size={20} />}
                                </div>
                            </div>

                            {expandedOrder === order.id && (
                                <div style={{ padding: '1.5rem', background: 'rgba(0,0,0,0.2)' }}>
                                    <h5 style={{ marginTop: 0, marginBottom: '1rem', color: 'var(--text-muted)' }}>Order Items</h5>
                                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                                        {order.items.map((item, idx) => (
                                            <div key={idx} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.95rem' }}>
                                                <span>{item.quantity}x {item.productName}</span>
                                                <span>${(item.unitPrice * item.quantity).toFixed(2)}</span>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
