# EasyCredit — Sistem Pengajuan Kredit Online

Sistem pengajuan kredit online sederhana dengan arsitektur dua database terpisah:
- **Frontend:** ASP.NET Core MVC · Razor Pages · jQuery AJAX
- **Backend:** ASP.NET Core Web API · JWT Authentication · API Key Protection
- **Data Master:** PostgreSQL (Entity Framework Core) untuk `MST_USER` dan `MST_CUSTOMER`
- **Data Transaksional:** Oracle Database (Dapper + Stored Procedure) untuk `TRN_CREDIT_APPLICATION` dan `TRN_CREDIT_INSTALLMENT`

---

## 1. Arsitektur & Tanggung Jawab Database

```
[ Browser / Pengguna ]
        │  (HTTP / AJAX)
        ▼
[ EasyCreditFrontend ] (ASP.NET Core MVC - Port 5166)
        │  (HttpClient · X-Api-Key · Bearer JWT)
        ▼
[ EasyCreditApi ]      (ASP.NET Core Web API - Port 5052)
   ├── PostgreSQL (EF Core)        ──> MST_USER, MST_CUSTOMER
   └── Oracle (Dapper + Stored Proc) ──> TRN_CREDIT_APPLICATION, TRN_CREDIT_INSTALLMENT
```

- **Tidak ada Foreign Key fisik lintas database.** Relasi `CUSTOMER_ID` divalidasi pada application/service layer.
- **Transaksional:** Insert header aplikasi kredit dan seluruh jadwal cicilan dieksekusi dalam **satu transaksi Oracle atomic**.
- **Stored Procedure:** Perhitungan simulasi cicilan (`SP_CALCULATE_INSTALLMENT`) dan persetujuan pinjaman (`SP_APPROVE_APPLICATION`, `SP_REJECT_APPLICATION`) dieksekusi langsung di Oracle engine.

---

## 2. Struktur Proyek

```
easy-credit/
├── EasyCreditApi/
│   ├── Controllers/               # API Controllers (REST)
│   ├── Application/
│   │   ├── DTO/                   # Request & Response DTOs
│   │   ├── Interfaces/            # Service contracts
│   │   └── Services/              # Business logic & validations
│   ├── Domain/Entities/           # Domain models
│   ├── Infrastructure/
│   │   ├── Postgres/              # AppDbContext & EfCustomerRepository
│   │   ├── Oracle/                # Dapper repository & SP runners
│   │   └── Security/              # BCrypt & JWT token generator
│   ├── Middleware/                # ApiKey, Exception, RequestLogging
│   └── Database/                  # Schema PostgreSQL & Oracle SP scripts
├── EasyCreditFrontend/
│   ├── Controllers/               # MVC Controllers
│   ├── Models/
│   │   ├── Dtos/                  # API transfer objects
│   │   └── ViewModels/            # Razor ViewModels
│   ├── Services/                  # ApiClient (Typed HttpClient)
│   ├── Views/                     # Razor Views & Partials
│   └── wwwroot/                   # CSS (Retro Print theme) & JS
├── docker-compose.yml             # Docker config for PostgreSQL & Oracle
└── README.md
```

---

## 3. Akun Demo

| Role | Email | Password | Hak Akses |
|---|---|---|---|
| **ADMIN** | `admin@easycredit.test` | `admin123` | Akses penuh (Customer CRUD, Ajukan Pinjaman, **Approve / Reject**) |
| **STAFF** | `staff@easycredit.test` | `staff123` | Customer CRUD, Ajukan Pinjaman (**Tidak bisa approve/reject — 403**) |

> *Catatan: Akun demo di atas otomatis di-seed saat aplikasi pertama kali dijalankan.*

---

## 4. Cara Setup & Menjalankan Aplikasi

### Prasyarat:
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Langkah 1: Jalankan Database (Docker Compose)
```bash
docker compose up -d
```
Pastikan container `easycredit_postgres` dan `easycredit_oracle` sudah berstatus `running`.

### Langkah 2: Jalankan Backend API
```bash
cd EasyCreditApi
dotnet run --urls "http://localhost:5052"
```
- **Scalar API Docs:** `http://localhost:5052/scalar/v1`
- **Swagger UI:** `http://localhost:5052/swagger`

### Langkah 3: Jalankan Frontend MVC
Buka terminal baru:
```bash
cd EasyCreditFrontend
dotnet run --urls "http://localhost:5166"
```
Akses aplikasi melalui browser di: `http://localhost:5166`

---

## 5. Kontrak API Minimum (12 Endpoint Wajib)

Semua endpoint `/api/*` dilindungi dengan header `X-Api-Key: easycredit-dev-api-key`.

| Method | Endpoint | Auth | Keterangan |
|---|---|---|---|
| `POST` | `/api/auth/login` | API Key | Login user & generate JWT Token |
| `POST` | `/api/auth/register` | API Key | Registrasi akun baru (opsional) |
| `GET` | `/api/customers` | API Key + JWT | Daftar customer (search & paging) |
| `GET` | `/api/customers/{id}` | API Key + JWT | Detail data customer by ID |
| `POST` | `/api/customers` | API Key + JWT | Tambah customer baru (NIK unik) |
| `PUT` | `/api/customers/{id}` | API Key + JWT | Perbarui data customer |
| `DELETE` | `/api/customers/{id}` | API Key + JWT | Hapus data customer |
| `GET` | `/api/credit-applications` | API Key + JWT | Daftar pengajuan kredit (filter status & search) |
| `GET` | `/api/credit-applications/{id}` | API Key + JWT | Detail pengajuan + daftar cicilan |
| `POST` | `/api/credit-applications/preview-installment` | API Key + JWT | Simulasi cicilan via SP Oracle (tanpa simpan) |
| `POST` | `/api/credit-applications` | API Key + JWT | Buat pengajuan + cicilan (1 transaksi Oracle) |
| `PUT` | `/api/credit-applications/{id}/approve` | API Key + ADMIN | Setujui pengajuan kredit via SP |
| `PUT` | `/api/credit-applications/{id}/reject` | API Key + ADMIN | Tolak pengajuan kredit via SP |

Semua response API dibungkus format seragam `ApiResponse<T>`:
```json
{
  "status": true,
  "message": "Success",
  "data": { ... }
}
```

---

## 6. Demo Checklist Kepatuhan Ketentuan

- [x] **`docker compose up -d`** berhasil menyalakan PostgreSQL & Oracle.
- [x] **API + Frontend** berjalan serentak di port masing-masing.
- [x] **Login ADMIN dan STAFF** berhasil dengan akun demo resmi.
- [x] **Customer CRUD** (PostgreSQL + EF Core) berfungsi penuh via AJAX.
- [x] **Preview cicilan** via AJAX memanggil `SP_CALCULATE_INSTALLMENT` tanpa reload dan tanpa menulis database.
- [x] **Create application** menyimpan header `TRN_CREDIT_APPLICATION` + baris `TRN_CREDIT_INSTALLMENT` dalam satu transaksi Oracle.
- [x] **Approve / Reject sebagai ADMIN** via Stored Procedure Oracle.
- [x] **Approve kedua kali ditolak** dengan HTTP 400 (`out_mess` Oracle).
- [x] **STAFF tidak bisa approve** (HTTP 403 Forbidden).
- [x] **Akses halaman tanpa login** langsung di-redirect ke `/Auth/Login`.
- [x] **Scalar API Documentation** interaktif lengkap dengan pengisian `X-Api-Key` dan `Bearer JWT`.
- [x] **README** lengkap dan dapat diikuti dari nol.
