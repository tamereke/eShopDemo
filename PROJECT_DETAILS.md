# eShopDemo Proje Yapısı ve Detaylı Açıklaması

Bu doküman, **eShopDemo** çözümündeki (solution) projelerin ne işe yaradığını, sorumluluklarını ve teknolojilerini detaylı bir şekilde açıklamaktadır. Proje, **.NET 8** ve **.NET Aspire** kullanılarak geliştirilmiş modern, olay tabanlı (event-driven) bir mikroservis mimarisidir.

## 🏗️ Temel Yapı ve Orkestrasyon

### 1. **AppHost (`src/AppHost`)**
*   **Rol:** Orkestratör (Orchestrator).
*   **Açıklama:** Tüm mikroservislerin, veritabanlarının (SQL Server, Redis, **Seq**) ve mesajlaşma altyapılarının (RabbitMQ, Kafka) tanımlandığı ve çalıştırıldığı yerdir.
*   **Görevleri:**
    *   Docker konteynerlerini yapılandırmak (örn: `rabbitmq-msg`).
    *   Merkezi Loglama Sunucusu (Seq) ve Dashboard'u yönetmek.
    *   Servisler arası dinamik port yönetimini sağlamak.

### 2. **ServiceDefaults (`src/ServiceDefaults`)**
*   **Rol:** Ortak Konfigürasyon Kütüphanesi.
*   **Açıklama:** Tüm servislerin loglama, metric ve resilience ayarlarını yönetir.
*   **Görevleri:**
    *   **Serilog:** Yapısal loglama (Structured Logging) ve Seq entegrasyonu.
    *   **OpenTelemetry:** Log, Metric ve Trace verilerinin toplanması.
    *   **Health Checks:** Servislerin sağlık durumlarının izlenmesi.
    *   **Resilience:** `Polly` politikaları ve HTTP yeniden deneme mekanizmaları.

### 3. **Gateway.Api (`src/Gateway.Api`)**
*   **Rol:** Reverse Proxy & SignalR Hub.
*   **Açıklama:** Web.UI (React) ile iç mikroservisler arasındaki tek giriş kapısıdır.
*   **Görevleri:**
    *   API yönlendirmesi (YARP).
    *   **SignalR EventHub:** Mikroservislerden gelen logları gerçek zamanlı olarak Frontend'e (Web UI Terminal) basmak.

---

## 🚀 Backend Mikroservisleri

### 4. **CatalogService.Api (`src/CatalogService.Api`)**
*   **Rol:** Ürün Kataloğu Yönetimi.
*   **Teknolojiler:** EF Core, SQL Server.
*   **Görevleri:** Ürün ve kategori listeleme, detay görme işlemleri.

### 5. **BasketService.Api (`src/BasketService.Api`)**
*   **Rol:** Alışveriş Sepeti Yönetimi.
*   **Teknolojiler:** Redis.
*   **Görevleri:** Ürünleri geçici olarak sepette tutmak.

### 6. **InventoryService.Api (`src/InventoryService.Api`)**
*   **Rol:** Stok Takibi.
*   **Görevleri:** Sipariş öncesi stok kontrolü sağlamak ve stok düşmek.

### 7. **OrderService.Api (`src/OrderService.Api`)**
*   **Rol:** Sipariş Yönetimi (Core Domain).
*   **Mimari:** Clean Architecture, CQRS, DDD.
*   **Teknolojiler:** MediatR, MassTransit (RabbitMQ & Kafka), EF Core, SQL Server.
*   **Görevleri:**
    *   **Sipariş Oluşturma:** Stok kontrolü yaparak siparişi oluşturur ve `OrderCreatedEvent` yayınlar.
    *   **Sipariş Onaylama/İptal:** Admin tarafından tetiklenir. Durum değişikliğini veritabanına yazar ve Kafka'ya `OrderConfirmedEvent` veya `OrderCancelledEvent` basar.
    *   RabbitMQ'yu iş akışı (workflow), Kafka'yı ise olay günlüğü (audit/notification) için kullanır.

### 8. **PaymentService.Api (`src/PaymentService.Api`)**
*   **Rol:** Ödeme İşlemleri (Simülasyon).
*   **Görevleri:** Ödeme başarımı simüle eder ve sonucu event olarak yayınlar.

---

## ⚙️ Arka Plan İşlemleri ve İletişim

### 9. **NotificationService.Worker (`src/NotificationService.Worker`)**
*   **Rol:** Asenkron Bildirim Servisi (Consumer).
*   **Teknolojiler:** MassTransit Kafka Consumer.
*   **Görevleri:**
    *   Kafka üzerindeki topicleri dinler: `order-events`, `order-confirmed`, `order-cancelled`, `payment-events`.
    *   Gelişmiş Console Loglama ile kullanıcıya SMS/E-posta atılıyormuş gibi simülasyon yapar.
    *   Development ortamında topicler yoksa otomatik oluşturur (CreateIfMissing).

### 10. **Shared.Contracts (`src/Shared.Contracts`)**
*   **Rol:** Ortak Veri Yapıları.
*   **İçerik:**
    *   **DTOs:** API veri modelleri.
    *   **Events:** `IntegrationEvent` (Abstract record), `OrderCreatedEvent`, `OrderConfirmedEvent` vb.
    *   **Polymorphism:** JSON serileştirme için polymorphic attribute konfigürasyonu içerir.

---

## 💻 Ön Yüz (Frontend)

### 11. **Web.UI (`src/Web.UI`)**
*   **Rol:** Kullanıcı Arayüzü (SPA).
*   **Teknolojiler:** React, Vite.
*   **Özellikler:**
    *   Ürün listeleme ve sepet yönetimi.
    *   **Admin Dashboard:** Siparişlerin durumuna göre filtrelenmesi (Pending, Approved, Cancelled) ve yönetilmesi.

---

## 🧪 Test Projeleri

*   **OrderService.Tests:** Domain Entity, CommandHandler ve Repository testlerini içerir. Mocking (Moq) kullanılarak dış bağımlılıklar (Inventory Service) simüle edilir.
*   **InventoryService.Tests** & **PaymentService.Tests:** İlgili servislerin birim testleri.
