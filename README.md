# ACO Termal Yazıcı Servis API & Dashboard emülatörü

Bu proje; **C# .NET 8** kullanılarak geliştirilmiş, **CQRS (MediatR)**, **FluentValidation** ve **Serilog** gibi modern kurumsal yazılım mimarilerini barındıran, mülakat gereksinimlerinin tamamını ve tüm opsiyonel bonus maddeleri karşılayan profesyonel bir **Termal Yazıcı Simülasyon Servisi** uygulamasıdır.

Uygulamanın içerisinde gömülü (embedded) olarak sunulan, canlı yazıcı animasyonlarına sahip, son derece şık, koyu mod (dark mode) ve cam efekti (glassmorphism) estetiği barındıran bir **Single Page Application (SPA) Web Arayüzü** ve entegre **Swagger API Dokümantasyonu** mevcuttur.

---

##  Proje Öne Çıkan Özellikleri (Bonus Maddeler Dahil)

1.  **Clean Architecture (Çoklu Proje)**: `Domain`, `Application`, `Infrastructure` ve `API` katmanları net şekilde ayrılmış olup, en iyi pratikler (ValidationBehaviour pipeline, ExceptionHandlingMiddleware, DTO yapısı) entegre edilmiştir.
2.  **Etkileşimli Swagger API Dokümantasyonu (X-Api-Token Destekli)**: `/swagger` ucu üzerinden tüm API test edilebilir. Üstelik Swagger arayüzüne eklediğimiz **ApiKey (X-Api-Token) güvenlik kilidi** sayesinde yetkilendirilmiş istekler doğrudan tarayıcıdan denenebilir.
3.  **Otomatik Yeniden Bağlanma (Exponential Backoff with Jitter)**: Bağlantı koptuğunda (`COMM_ERROR`), yazıcı otomatik olarak katlanarak artan sürelerle ($1s \rightarrow 2s \rightarrow 4s \rightarrow 8s \rightarrow 16s \rightarrow 30s$ + %10 rastgele jitter gürültüsü) yeniden bağlanmayı dener. Arayüzde bu geri sayım canlı izlenebilir.
4.  **Kuyruk ve Idempotency Kontrolü**: Tüm baskı istekleri thread-safe `ConcurrentQueue` ile sıraya alınır. Gelen işlerde mükerrer basımı engellemek amacıyla `JobId` veya header'dan iletilen `Idempotency-Key` üzerinden kontrol sağlanır.
5.  **Rulo ve ETA Tahminleme (Prediction)**: 50 metrelik sanal fiş rulosu; metin satırları ($0.15\text{ cm}$), QR kodlar ($1.5\text{ cm}$) ve görsellerin ($2.5\text{ cm}$) tüketimlerine göre santimetre hassasiyetinde takip edilir. Kalan rulo yüzdesine göre basılabilecek tahmini fiş adedini ve aktif işlerin **ETA (yazım süresi)** değerleri dinamik hesaplanır.
6.  **Loglama & CSV Aktarımı**: Başarılı ve başarısız tüm operasyonlar mülakat şemasına tam uyumlu olarak `logs.json` dosyasına kaydedilir. `GET /api/logs/csv` ucundan bu loglar tek tıkla CSV olarak indirilebilir.
7.  **Basit Yetkilendirme (Token)**: Güvenlik amacıyla `.env` dosyasında tanımlanan `API_TOKEN` değeri, mutating (POST) işlemlerde `X-Api-Token` header'ı üzerinden doğrulanır.
8.  **Dinamik Hata Simülasyonu**: Değerlendiricinin API'nin hata yönetimini sınayabilmesi için arayüzde bir **Hata Simülatörü Kontrol Paneli** yer alır. Tek tıkla `PAPER_OUT`, `PAPER_JAM`, `COVER_OPEN`, `OVERHEAT` veya `COMM_ERROR` hataları tetiklenebilir.

---

##  Proje Katman Yapısı

```
AcoTestApi/
├── AcoTestApi.sln                     # Çözüm Dosyası
└── src/
    ├── AcoTestApi.Domain/             # İş kuralları, model varlıkları ve enumlar
    ├── AcoTestApi.Application/        # CQRS (MediatR), FluentValidation, Arayüzler
    ├── AcoTestApi.Infrastructure/     # Yazıcı simülasyonu, Kuyruk HostedService, Prediction
    ├── AcoTestApi.API/                # Web API Controller, Exception/Auth Middleware, wwwroot UI
    └── AcoTestApi.Tests/              # xUnit, NSubstitute ve FluentAssertions Test Paketi (22 Test)
```

---

##  Visual Studio ile Başlatma Kılavuzu

Projemiz Visual Studio 2022 veya üzeri sürümlerle tam uyumludur. Visual Studio üzerinden başlatmak için şu adımları izleyin:

1.  Ana dizinde yer alan **`AcoTestApi.sln`** dosyasına çift tıklayarak çözümü Visual Studio'da açın.
2.  Visual Studio'nun sağ tarafındaki *Solution Explorer (Çözüm Gezgini)* panelinden **`AcoTestApi.API`** projesine sağ tıklayın ve **Set as Startup Project (Başlangıç Projesi Olarak Ayarla)** seçeneğini seçin.
3.  Visual Studio üst menüsündeki yeşil başlatma butonunun (Debug) yanındaki aşağı açılır menüden **`http`** veya **`IIS Express`** profilini seçin.
4.  Klavye üzerinden **F5** tuşuna basarak veya yeşil **Start** butonuna tıklayarak uygulamayı debug modunda başlatın.
5.  Uygulama ayağa kalktığında otomatik olarak bir tarayıcı penceresi açılacak ve sizi gömülü arayüze yönlendirecektir:
    *   **Dashboard Arayüzü**: `http://localhost:8080` (veya Visual Studio'nun atadığı rastgele localhost portu)
    *   **Interactive Swagger API Dokümantasyonu**: `http://localhost:8080/swagger` (veya `.../swagger` ucu)

---

##  Alternatif Başlatma Yolları (Terminal / Docker)

### Yöntem A: Terminal/PowerShell Üzerinden Başlatma (Tek Komut)

1.  Projenin ana klasörüne (`AcoTestApi/`) gidin.
2.  Aşağıdaki tek komutu çalıştırarak projeyi derleyip anında ayağa kaldırın:
    ```bash
    dotnet run --project src/AcoTestApi.API/AcoTestApi.API.csproj
    ```
3.  Tarayıcınızdan **`http://localhost:8080`** adresine giderek canlı dashboard ve emülatörü kullanmaya başlayın!

### Yöntem B: Docker ve Docker Compose ile Çalıştırma

1.  Uygulamayı izole bir konteynerde derleyip çalıştırmak için ana dizinde şu komutu verin:
    ```bash
    docker-compose up --build -d
    ```
2.  Uygulama arka planda saniyeler içinde ayağa kalkacak ve yine **`http://localhost:8080`** adresinden erişilebilir olacaktır.

---

##  API Test & Entegrasyon Kılavuzu (Postman & Curl Örnekleri)

Yetkilendirme aktifken mutating isteklerde `X-Api-Token: aco-secret-token` başlığı gönderilmelidir. `.env` dosyasında veya Swagger Authorize penceresinde bu değeri kullanabilirsiniz.

### 1. Yazıcıya Bağlanma (`POST /connect`)
```bash
curl -X POST http://localhost:8080/connect \
  -H "Content-Type: application/json" \
  -H "X-Api-Token: aco-secret-token" \
  -d '{"mode": "usb"}'
```

### 2. Canlı Durum Sorgulama (`GET /status`)
```bash
curl -X GET http://localhost:8080/status
```

### 3. Metin Yazdırma (`POST /print/text`)
```bash
curl -X POST http://localhost:8080/print/text \
  -H "Content-Type: application/json" \
  -H "X-Api-Token: aco-secret-token" \
  -d '{"text": "ACO Geri Dönüşüm Sistemleri\n-----------------\nMülakat Projesi Basım Testi", "language": "tr", "jobId": "job123"}'
```

### 4. QR Kod Yazdırma (`POST /print/qr`)
```bash
curl -X POST http://localhost:8080/print/qr \
  -H "Content-Type: application/json" \
  -H "X-Api-Token: aco-secret-token" \
  -d '{"qrData": "https://acorecycling.com", "jobId": "qr456"}'
```

---

## 💡 Birim Testleri Çalıştırma
Tüm iş kuralları, prediction algoritmaları ve simülatör durumları test kapsamına alınmıştır. Testleri koşturmak için:
```bash
dotnet test
```
*(Toplamda 24 unit testin tamamı başarıyla geçmektedir.)*
