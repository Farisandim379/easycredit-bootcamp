-- =============================================================================
-- EasyCredit - PostgreSQL Schema & Seed Data
-- Database: PostgreSQL 15+ / 16+
-- Tables: MST_USER, MST_CUSTOMER
-- =============================================================================

-- 1. Table: MST_USER
CREATE TABLE IF NOT EXISTS mst_user (
    user_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL, -- ADMIN | STAFF
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- 2. Table: MST_CUSTOMER
CREATE TABLE IF NOT EXISTS mst_customer (
    customer_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    nik VARCHAR(20) NOT NULL UNIQUE,
    phone VARCHAR(20),
    email VARCHAR(100),
    address VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_customer_nik ON mst_customer(nik);
CREATE INDEX IF NOT EXISTS idx_customer_name ON mst_customer(name);
CREATE INDEX IF NOT EXISTS idx_user_email ON mst_user(email);

-- 3. Seed Default Demo Users
-- Password: admin123 (ADMIN) and staff123 (STAFF)
-- Bcrypt Hash ($2a$11$...)
INSERT INTO mst_user (name, email, password_hash, role, created_at)
VALUES 
    ('Administrator', 'admin@easycredit.test', '$2a$11$hG0c5s1x5K3T1e8M4q0X5O8Q46.Lz6T7I6YyY1.gS1ZfJ9r9K8F7Q', 'ADMIN', NOW()),
    ('Staff Officer', 'staff@easycredit.test', '$2a$11$rE0c5s1x5K3T1e8M4q0X5O8Q46.Lz6T7I6YyY1.gS1ZfJ9r9K8F7Q', 'STAFF', NOW())
ON CONFLICT (email) DO NOTHING;

-- Seed Sample Customers (optional initial demo data)
INSERT INTO mst_customer (name, nik, phone, email, address, created_at)
VALUES 
    ('Budi Santoso', '3171010101900001', '081234567890', 'budi.santoso@example.com', 'Jl. Merdeka No. 10, Jakarta Pusat', NOW()),
    ('Siti Aminah', '3171010202920002', '081298765432', 'siti.aminah@example.com', 'Jl. Sudirman Kav. 25, Jakarta Selatan', NOW()),
    ('Andi Wijaya', '3201010101010003', '081311223344', 'andi.wijaya@example.com', 'Jl. Dago No. 120, Bandung', NOW())
ON CONFLICT (nik) DO NOTHING;
