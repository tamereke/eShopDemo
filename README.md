# eShop Demo - Real-Time Event-Driven Microservices 🚀

Production-grade, real-time event-driven e-ticaret mikroservis uygulaması. **.NET 10**, **.NET Aspire**, **SignalR**, **React (Vite)**, Clean Architecture, DDD ve CQRS pattern'leri ile geliştirilmiştir.

## 🎯 Mimari Genel Bakış

```
┌─────────────┐   ┌─────────────┐       ┌─────────────┐
│   Web UI    │ ──►   Gateway   │ ◄──── ►     Seq     │ (Merkezi Loglama)
└──────┬──────┘   └──┬───┬──────┘       └─────────────┘
       │             │   │
       │             │   └───────────────┐ (Canlı Log Akışı - SignalR)
       │             │                   ▼
       │       ┌───┴────┬────────┬──────────┐
       │       │        │        │          │
    ┌──▼───┐ ┌─▼────┐ ┌─▼──────┐ ┌─▼───────────┐
    │Order │ │Inven-│ │Payment │ │Notification │
    │      │ │tory  │ │        │ │  (Worker)   │
    └──┬───┘ └──┬───┘ └──┬─────┘ └─────────────┘
       │        │        │              ▲
       │   RabbitMQ (Commands)          │
       └────────┴────────┴──────────────┘
                │
            Kafka (Audit Events)
                │
                └──────────────────────►
```

### Event Flow

1. **Sipariş Oluşturma**: Gateway → Order Service (POST /orders)
2. **OrderCreatedEvent**: RabbitMQ (İş Akışı) + Kafka (Audit)
3. **Stok Rezervasyonu**: Inventory Service (consumer)
4. **StockReservedEvent**: RabbitMQ + Kafka
5. **Ödeme İşleme**: Payment Service (consumer)
6. **PaymentProcessedEvent**: RabbitMQ + Kafka
7. **Sipariş Onay/İptal**: Admin UI → Order Service
   - **OrderConfirmedEvent**: Kafka (Notification Worker dinler)
   - **OrderCancelledEvent**: Kafka (Notification Worker dinler)
8. **Bildirim**: Notification Service (Kafka consumer -> Console Log Simülasyonu)
9. **Canlı İzleme**: ServiceLogs → Gateway (SignalR) → Web UI Terminal

## 🛠️ Teknoloji Stack

- **.NET 10** - Backend Framework
- **React + Vite** - Frontend (Admin Dashboard)
- **.NET Aspire** - Orchestration & Service Discovery
- **Seq** - Merkezi Loglama ve Analiz
- **Serilog** - Yapısal Loglama (Structured Logging)
- **MSSQL** - Order Service database
- **Redis** - Distributed cache
- **RabbitMQ** - Command/Workflow events
- **Kafka** - Event streaming & audit storage
- **MassTransit** - Messaging abstraction (RabbitMQ & Kafka Riders)
- **EF Core** - ORM
- **MediatR** - CQRS pattern
- **Docker** - Containerization
- **xUnit + Moq + FluentAssertions** - Testing

## 📁 Proje Yapısı

```
eShopDemo/
├── src/
│   ├── AppHost/                      # Aspire orchestration
│   ├── ServiceDefaults/              # Shared Aspire configs
│   ├── Shared.Contracts/             # Shared DTOs & Events
│   ├── Gateway.Api/                  # API Gateway (BFF)
│   ├── OrderService.Api/             # Order microservice
│   ├── InventoryService.Api/         # Inventory microservice
│   ├── PaymentService.Api/           # Payment microservice
│   ├── NotificationService.Worker/   # Notification worker
│   └── Web.UI/                       # React Frontend
├── tests/
│   ├── OrderService.Tests/           # Unit tests
│   ├── InventoryService.Tests/
│   └── PaymentService.Tests/
└── docker-compose.yaml
```

## 🚀 Hızlı Başlangıç

### Ön Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js](https://nodejs.org/) (Frontend için)
- **Aspire Workload**:
  ```bash
  dotnet workload install aspire
  ```

### Çalıştırma

**Aspire ile Çalıştırma (Önerilen)**

```bash
dotnet run --project src/AppHost/AppHost.csproj
```

Bu komut tüm backend servisleri, veritabanlarını, mesaj kuyruklarını (RabbitMQ, Kafka) ve Frontend uygulamasını başlatır.

- **Web UI**: http://localhost:5173
- **Aspire Dashboard**: http://localhost:15001 (veya konsolda belirtilen port)
- **Gateway API**: http://localhost:5000

## 🧪 Test

Proje, özellikle kritik iş mantığı içeren `OrderService` için kapsamlı unit testlere sahiptir.

```bash
# Tüm testleri çalıştır
dotnet test

# Sadece Order Service testleri
dotnet test tests/OrderService.Tests/OrderService.Tests.csproj
```

**Kapsanan Senaryolar:**
- ✅ **Domain Logic**: Order aggregate davranışları, item ekleme, durum değişimleri.
- ✅ **Application Logic**: `CreateOrderCommandHandler` ile stok kontrolü simülasyonu (Mock HTTP Client).
- ✅ **Repository Logic**: In-memory DB ile `GetByStatusAsync`, `GetByCustomerIdAsync` testleri.

## � Kafka & Event Configuration

Sistemde güvenilir mesajlaşma için hem RabbitMQ hem de Kafka hibrit yapıda kullanılmaktadır.

### Topic Yapısı
- **`order-events`**: Genel sipariş olayları (Created).
- **`order-confirmed`**: Sipariş onaylandığında tetiklenir.
- **`order-cancelled`**: Sipariş iptal edildiğinde tetiklenir.
- **`inventory-events`**: Stok hareketleri.
- **`payment-events`**: Ödeme işlemleri.

### Özellikler
- **Auto-Create Topics**: Geliştirme ortamında (Aspire), Kafka topicleri Producer veya Consumer (Worker) tarafından otomatik oluşturulacak şekilde yapılandırılmıştır.
- **Polymorphic Events**: `IntegrationEvent` tabanlı olaylar, `System.Text.Json` polymorphism desteği ile tip güvenli bir şekilde işlenir.
- **Notification Worker**: Tüm bu topicleri dinler ve kullanıcı bildirimlerini simüle eder (Console Log).

## 🎨 Clean Architecture & DDD

### Order Service Katmanları

```
OrderService.Api/
├── Domain/              # Entities (Order), Value Objects, Enums
├── Application/         # Commands (CreateOrder), Queries, Handlers
└── Infrastructure/      # EF Core, Repositories, Redis Cache
```

### Domain-Driven Design
- **Aggregates**: Order (root)
- **Domain Events**: OrderCreatedEvent, OrderConfirmedEvent, OrderCancelledEvent
- **Business Rules**: Stok kontrolü (CommandHandler içinde), Sipariş iptal kuralları (Domain entity içinde).

## 🏥 Health Checks & Monitoring

Aspire Dashboard üzerinden tüm container'ların (SQL, Redis, Kafka, RabbitMQ) ve .NET servislerinin sağlık durumunu, loglarını ve distributed trace'lerini canlı izleyebilirsiniz.

### 📊 Gelişmiş Loglama ve İzleme

Proje, üretim seviyesinde (production-grade) bir izlenebilirlik altyapısına sahiptir:

1.  **Merkezi Loglama (Seq)**:
    *   Tüm mikroservisler loglarını yapısal (JSON) formatında tek bir **Seq** sunucusuna gönderir.
    *   Seq arayüzünden (`http://localhost:5341`) loglar filtrelenebilir ve analiz edilebilir.
    *   Serilog `Enricher`'lar kullanılarak loglara `ServiceName` gibi etiketler otomatik eklenir.

2.  **Canlı Terminal (SignalR)**:
    *   Mikroservisler önemli iş olaylarını (ör. "Sipariş Oluşturuldu") Gateway üzerindeki `/api/monitor/send` endpoint'ine gönderir.
    *   Gateway, bu logları **SignalR** üzerinden Web Arayüzüne (Terminal ekranı) **anlık** olarak basar.

3.  **Resilience & Noise Reduction (Gürültü Azaltma)**:
    *   `Polly` ve `HttpClient`'tan gelen gürültülü (verbose) loglar filtrelenir.
    *   RabbitMQ bağlantı hataları ve retry mekanizmaları optimize edilmiştir.

---

**Geliştirici Notu:**
Bu proje, mikroservis mimarisindeki "Eventually Consistency", "Event Sourcing" (kısmen) ve "Resiliency" kavramlarını pratik bir şekilde göstermek amacıyla hazırlanmıştır.
