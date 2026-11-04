-- =============================================================================
-- EasyCredit - Oracle Database Schema & Stored Procedures
-- Database: Oracle XE / Free / 19c / 21c (Schema: TRAINING)
-- Tables: TRN_CREDIT_APPLICATION, TRN_CREDIT_INSTALLMENT
-- =============================================================================

-- 1. Sequences
BEGIN
    EXECUTE IMMEDIATE 'CREATE SEQUENCE TRAINING.SEQ_TRN_CREDIT_APP START WITH 1 INCREMENT BY 1 NOCACHE';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -955 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'CREATE SEQUENCE TRAINING.SEQ_TRN_CREDIT_INST START WITH 1 INCREMENT BY 1 NOCACHE';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -955 THEN RAISE; END IF;
END;
/

-- 2. Table: TRN_CREDIT_APPLICATION
CREATE TABLE TRAINING.TRN_CREDIT_APPLICATION (
    APPLICATION_ID    NUMBER PRIMARY KEY,
    CUSTOMER_ID       NUMBER NOT NULL,             -- Referensi ke PostgreSQL (tanpa FK fisik)
    LOAN_AMOUNT       NUMBER NOT NULL,
    TENOR_MONTHS      NUMBER NOT NULL,             -- 6 / 12 / 24 / 36
    INTEREST_RATE     NUMBER DEFAULT 10,           -- Default bunga 10%
    CREDIT_SCORE      NUMBER DEFAULT 700,          -- Skor internal (default 700)
    STATUS            VARCHAR2(20) DEFAULT 'PENDING', -- PENDING | APPROVED | REJECTED
    APPLICATION_DATE  DATE DEFAULT SYSDATE NOT NULL,
    APPROVED_BY       VARCHAR2(100),
    APPROVED_AT       DATE
);
/

-- 3. Table: TRN_CREDIT_INSTALLMENT
CREATE TABLE TRAINING.TRN_CREDIT_INSTALLMENT (
    INSTALLMENT_ID     NUMBER PRIMARY KEY,
    APPLICATION_ID     NUMBER NOT NULL REFERENCES TRAINING.TRN_CREDIT_APPLICATION(APPLICATION_ID) ON DELETE CASCADE,
    INSTALLMENT_NO     NUMBER NOT NULL,
    DUE_DATE           DATE NOT NULL,
    INSTALLMENT_AMOUNT NUMBER NOT NULL,
    STATUS             VARCHAR2(20) DEFAULT 'UNPAID'  -- UNPAID | PAID
);
/

CREATE INDEX TRAINING.IDX_CR_APP_CUST ON TRAINING.TRN_CREDIT_APPLICATION(CUSTOMER_ID);
CREATE INDEX TRAINING.IDX_CR_INST_APP ON TRAINING.TRN_CREDIT_INSTALLMENT(APPLICATION_ID);
/

-- =============================================================================
-- STORED PROCEDURE: SP_CALCULATE_INSTALLMENT
-- Menghitung simulasi cicilan tanpa menyimpan data ke tabel
-- =============================================================================
CREATE OR REPLACE PROCEDURE TRAINING.SP_CALCULATE_INSTALLMENT (
    p_loan_amount   IN  NUMBER,
    p_tenor_months  IN  NUMBER,
    p_cursor        OUT SYS_REFCURSOR,
    out_stat        OUT VARCHAR2,
    out_mess        OUT VARCHAR2
) AS
    v_monthly_interest_rate NUMBER := (10 / 100) / 12;
    v_monthly_amount        NUMBER;
BEGIN
    -- Validasi nominal (Rp 1.000.000 s/d Rp 50.000.000)
    IF p_loan_amount < 1000000 OR p_loan_amount > 50000000 THEN
        out_stat := 'ERROR';
        out_mess := 'Jumlah pinjaman harus antara Rp 1.000.000 dan Rp 50.000.000.';
        OPEN p_cursor FOR SELECT NULL FROM dual WHERE 1=0;
        RETURN;
    END IF;

    -- Validasi tenor (6, 12, 24, 36)
    IF p_tenor_months NOT IN (6, 12, 24, 36) THEN
        out_stat := 'ERROR';
        out_mess := 'Tenor harus 6, 12, 24, atau 36 bulan.';
        OPEN p_cursor FOR SELECT NULL FROM dual WHERE 1=0;
        RETURN;
    END IF;

    -- Rumus flat/anuitas sederhana: Pokok / Tenor + Bunga per bulan
    v_monthly_amount := ROUND((p_loan_amount / p_tenor_months) + (p_loan_amount * v_monthly_interest_rate));

    OPEN p_cursor FOR
        SELECT 
            LEVEL AS INSTALLMENT_NO,
            ADD_MONTHS(TRUNC(SYSDATE), LEVEL) AS DUE_DATE,
            v_monthly_amount AS INSTALLMENT_AMOUNT,
            'UNPAID' AS STATUS
        FROM dual
        CONNECT BY LEVEL <= p_tenor_months;

    out_stat := 'SUCCESS';
    out_mess := 'Perhitungan simulasi cicilan berhasil.';
EXCEPTION
    WHEN OTHERS THEN
        out_stat := 'ERROR';
        out_mess := SQLERRM;
        OPEN p_cursor FOR SELECT NULL FROM dual WHERE 1=0;
END;
/

-- =============================================================================
-- STORED PROCEDURE: SP_APPROVE_APPLICATION
-- Menyetujui pengajuan kredit secara transaksional
-- =============================================================================
CREATE OR REPLACE PROCEDURE TRAINING.SP_APPROVE_APPLICATION (
    p_app_id    IN  NUMBER,
    p_approver  IN  VARCHAR2,
    out_stat    OUT VARCHAR2,
    out_mess    OUT VARCHAR2
) AS
    v_current_status VARCHAR2(20);
BEGIN
    SELECT STATUS INTO v_current_status
    FROM TRAINING.TRN_CREDIT_APPLICATION
    WHERE APPLICATION_ID = p_app_id;

    IF v_current_status IN ('SUBMITTED', 'PENDING') THEN
        UPDATE TRAINING.TRN_CREDIT_APPLICATION
        SET STATUS = 'APPROVED',
            APPROVED_BY = p_approver,
            APPROVED_AT = SYSDATE
        WHERE APPLICATION_ID = p_app_id;

        COMMIT;
        out_stat := 'SUCCESS';
        out_mess := 'Pengajuan pinjaman #' || p_app_id || ' berhasil disetujui.';
    ELSIF v_current_status IN ('APPROVED', 'REJECTED') THEN
        out_stat := 'INVALID_STATUS';
        out_mess := 'Pengajuan kredit #' || p_app_id || ' sudah berstatus ' || v_current_status || ' dan tidak dapat diproses lagi.';
    ELSE
        out_stat := 'INVALID_STATUS';
        out_mess := 'Status pengajuan saat ini (' || v_current_status || ') tidak memenuhi syarat untuk disetujui.';
    END IF;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        out_stat := 'NOT_FOUND';
        out_mess := 'Pengajuan kredit #' || p_app_id || ' tidak ditemukan.';
    WHEN OTHERS THEN
        ROLLBACK;
        out_stat := 'ERROR';
        out_mess := SQLERRM;
END;
/

-- =============================================================================
-- STORED PROCEDURE: SP_REJECT_APPLICATION
-- Menolak pengajuan kredit secara transaksional
-- =============================================================================
CREATE OR REPLACE PROCEDURE TRAINING.SP_REJECT_APPLICATION (
    p_app_id    IN  NUMBER,
    p_approver  IN  VARCHAR2,
    out_stat    OUT VARCHAR2,
    out_mess    OUT VARCHAR2
) AS
    v_current_status VARCHAR2(20);
BEGIN
    SELECT STATUS INTO v_current_status
    FROM TRAINING.TRN_CREDIT_APPLICATION
    WHERE APPLICATION_ID = p_app_id;

    IF v_current_status IN ('SUBMITTED', 'PENDING') THEN
        UPDATE TRAINING.TRN_CREDIT_APPLICATION
        SET STATUS = 'REJECTED',
            APPROVED_BY = p_approver,
            APPROVED_AT = SYSDATE
        WHERE APPLICATION_ID = p_app_id;

        COMMIT;
        out_stat := 'SUCCESS';
        out_mess := 'Pengajuan pinjaman #' || p_app_id || ' telah ditolak.';
    ELSIF v_current_status IN ('APPROVED', 'REJECTED') THEN
        out_stat := 'INVALID_STATUS';
        out_mess := 'Pengajuan kredit #' || p_app_id || ' sudah berstatus ' || v_current_status || ' dan tidak dapat diproses lagi.';
    ELSE
        out_stat := 'INVALID_STATUS';
        out_mess := 'Status pengajuan saat ini (' || v_current_status || ') tidak memenuhi syarat untuk ditolak.';
    END IF;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        out_stat := 'NOT_FOUND';
        out_mess := 'Pengajuan kredit #' || p_app_id || ' tidak ditemukan.';
    WHEN OTHERS THEN
        ROLLBACK;
        out_stat := 'ERROR';
        out_mess := SQLERRM;
END;
/
