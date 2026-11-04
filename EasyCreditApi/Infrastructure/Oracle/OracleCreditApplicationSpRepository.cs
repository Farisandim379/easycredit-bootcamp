using System.Data;
using Dapper;
using Dapper.Oracle;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure.Oracle;

namespace EasyCreditApi.Infrastructure;

public sealed class OracleCreditApplicationSpRepository : ICreditApplicationSpRepository
{
    private const string Schema = "TRAINING";
    private readonly OracleConnectionFactory _factory;

    public OracleCreditApplicationSpRepository(OracleConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<CreditApplication>> GetAllAsync(string? status, string? keyword)
    {
        await using var conn = _factory.Create();

        var sql = @"
            SELECT 
                APPLICATION_ID AS ApplicationId,
                CUSTOMER_ID AS CustomerId,
                LOAN_AMOUNT AS Amount,
                TENOR_MONTHS AS TenorMonths,
                INTEREST_RATE AS InterestRate,
                CREDIT_SCORE AS CreditScore,
                STATUS AS Status,
                APPLICATION_DATE AS ApplicationDate,
                APPROVED_BY AS ApprovedBy,
                APPROVED_AT AS ApprovedAt
            FROM TRAINING.TRN_CREDIT_APPLICATION
            WHERE (1=1)";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " AND UPPER(STATUS) = UPPER(:Status)";
            parameters.Add("Status", status.Trim());
        }

        sql += " ORDER BY APPLICATION_ID DESC";

        var rows = await conn.QueryAsync<CreditApplication>(sql, parameters);
        return rows;
    }

    public async Task<CreditApplication?> GetByIdAsync(int id)
    {
        await using var conn = _factory.Create();

        var sqlHeader = @"
            SELECT 
                APPLICATION_ID AS ApplicationId,
                CUSTOMER_ID AS CustomerId,
                LOAN_AMOUNT AS Amount,
                TENOR_MONTHS AS TenorMonths,
                INTEREST_RATE AS InterestRate,
                CREDIT_SCORE AS CreditScore,
                STATUS AS Status,
                APPLICATION_DATE AS ApplicationDate,
                APPROVED_BY AS ApprovedBy,
                APPROVED_AT AS ApprovedAt
            FROM TRAINING.TRN_CREDIT_APPLICATION
            WHERE APPLICATION_ID = :Id";

        var header = await conn.QueryFirstOrDefaultAsync<CreditApplication>(sqlHeader, new { Id = id });
        if (header == null)
            return null;

        var sqlInstallments = @"
            SELECT
                INSTALLMENT_ID AS InstallmentId,
                APPLICATION_ID AS ApplicationId,
                INSTALLMENT_NO AS InstallmentNo,
                DUE_DATE AS DueDate,
                INSTALLMENT_AMOUNT AS InstallmentAmount,
                STATUS AS Status
            FROM TRAINING.TRN_CREDIT_INSTALLMENT
            WHERE APPLICATION_ID = :Id
            ORDER BY INSTALLMENT_NO ASC";

        var installments = await conn.QueryAsync<CreditInstallment>(sqlInstallments, new { Id = id });
        header.Installments = installments.ToList();

        return header;
    }

    public async Task<IEnumerable<CreditInstallment>> CalculateInstallmentsAsync(decimal amount, int tenorMonths)
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_loan_amount", amount, OracleMappingType.Decimal, ParameterDirection.Input);
        p.Add("p_tenor_months", tenorMonths, OracleMappingType.Int32, ParameterDirection.Input);
        p.Add("p_cursor", dbType: OracleMappingType.RefCursor, direction: ParameterDirection.Output);
        p.Add("out_stat", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 50);
        p.Add("out_mess", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 500);

        var items = await conn.QueryAsync<dynamic>(
            $"{Schema}.SP_CALCULATE_INSTALLMENT",
            p,
            commandType: CommandType.StoredProcedure);

        var status = p.Get<string>("out_stat");
        var message = p.Get<string>("out_mess");

        if (status != "SUCCESS")
        {
            throw new InvalidOperationException(message ?? "Gagal menghitung simulasi cicilan.");
        }

        return items.Select(x => new CreditInstallment
        {
            InstallmentNo = (int)x.INSTALLMENT_NO,
            DueDate = (DateTime)x.DUE_DATE,
            InstallmentAmount = (decimal)x.INSTALLMENT_AMOUNT,
            Status = (string)(x.STATUS ?? "UNPAID")
        });
    }

    public async Task<(string Status, string Message, int ApplicationId)> CreateTransactionalAsync(
        CreditApplication application,
        IEnumerable<CreditInstallment> installments)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var pHeader = new OracleDynamicParameters();
            pHeader.Add("CustomerId", application.CustomerId, OracleMappingType.Int32, ParameterDirection.Input);
            pHeader.Add("Amount", application.Amount, OracleMappingType.Decimal, ParameterDirection.Input);
            pHeader.Add("TenorMonths", application.TenorMonths, OracleMappingType.Int32, ParameterDirection.Input);
            pHeader.Add("InterestRate", application.InterestRate ?? 10, OracleMappingType.Decimal, ParameterDirection.Input);
            pHeader.Add("CreditScore", application.CreditScore ?? 700, OracleMappingType.Int32, ParameterDirection.Input);
            pHeader.Add("Status", application.Status ?? "PENDING", OracleMappingType.Varchar2, ParameterDirection.Input);
            pHeader.Add("out_id", dbType: OracleMappingType.Int32, direction: ParameterDirection.Output);

            var sqlInsertHeader = @"
                INSERT INTO TRAINING.TRN_CREDIT_APPLICATION (
                    APPLICATION_ID,
                    CUSTOMER_ID,
                    LOAN_AMOUNT,
                    TENOR_MONTHS,
                    INTEREST_RATE,
                    CREDIT_SCORE,
                    STATUS,
                    APPLICATION_DATE
                ) VALUES (
                    TRAINING.SEQ_TRN_CREDIT_APP.NEXTVAL,
                    :CustomerId,
                    :Amount,
                    :TenorMonths,
                    :InterestRate,
                    :CreditScore,
                    :Status,
                    SYSDATE
                ) RETURNING APPLICATION_ID INTO :out_id";

            await conn.ExecuteAsync(sqlInsertHeader, pHeader, transaction: tx);
            var appId = pHeader.Get<int>("out_id");
            application.ApplicationId = appId;

            var sqlInsertInst = @"
                INSERT INTO TRAINING.TRN_CREDIT_INSTALLMENT (
                    INSTALLMENT_ID,
                    APPLICATION_ID,
                    INSTALLMENT_NO,
                    DUE_DATE,
                    INSTALLMENT_AMOUNT,
                    STATUS
                ) VALUES (
                    TRAINING.SEQ_TRN_CREDIT_INST.NEXTVAL,
                    :AppId,
                    :InstallmentNo,
                    :DueDate,
                    :InstallmentAmount,
                    :Status
                )";

            foreach (var inst in installments)
            {
                var pInst = new OracleDynamicParameters();
                pInst.Add("AppId", appId, OracleMappingType.Int32, ParameterDirection.Input);
                pInst.Add("InstallmentNo", inst.InstallmentNo, OracleMappingType.Int32, ParameterDirection.Input);
                pInst.Add("DueDate", inst.DueDate, OracleMappingType.Date, ParameterDirection.Input);
                pInst.Add("InstallmentAmount", inst.InstallmentAmount, OracleMappingType.Decimal, ParameterDirection.Input);
                pInst.Add("Status", inst.Status ?? "UNPAID", OracleMappingType.Varchar2, ParameterDirection.Input);

                await conn.ExecuteAsync(sqlInsertInst, pInst, transaction: tx);
            }

            tx.Commit();
            return ("SUCCESS", "Pengajuan pinjaman beserta jadwal cicilan berhasil dibuat.", appId);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return ("ERROR", ex.Message, 0);
        }
    }

    public async Task<(string Status, string Message)> ApproveAsync(int applicationId, string approver)
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_app_id", applicationId, OracleMappingType.Int32, ParameterDirection.Input);
        p.Add("p_approver", approver, OracleMappingType.Varchar2, ParameterDirection.Input, 100);
        p.Add("out_stat", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 50);
        p.Add("out_mess", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 500);

        await conn.ExecuteAsync(
            $"{Schema}.SP_APPROVE_APPLICATION",
            p,
            commandType: CommandType.StoredProcedure);

        var status = p.Get<string>("out_stat") ?? "ERROR";
        var message = p.Get<string>("out_mess") ?? string.Empty;

        return (status, message);
    }

    public async Task<(string Status, string Message)> RejectAsync(int applicationId, string approver)
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_app_id", applicationId, OracleMappingType.Int32, ParameterDirection.Input);
        p.Add("p_approver", approver, OracleMappingType.Varchar2, ParameterDirection.Input, 100);
        p.Add("out_stat", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 50);
        p.Add("out_mess", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 500);

        await conn.ExecuteAsync(
            $"{Schema}.SP_REJECT_APPLICATION",
            p,
            commandType: CommandType.StoredProcedure);

        var status = p.Get<string>("out_stat") ?? "ERROR";
        var message = p.Get<string>("out_mess") ?? string.Empty;

        return (status, message);
    }
}
