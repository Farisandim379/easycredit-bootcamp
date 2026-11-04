// [DAY 6] Repository Credit Application berbasis Oracle Stored Procedure (Dapper.Oracle)
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

    public async Task<IEnumerable<CreditApplication>> GetAllAsync()
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_cursor", dbType: OracleMappingType.RefCursor, direction: ParameterDirection.Output);

        return await conn.QueryAsync<CreditApplication>(
            $"{Schema}.sp_get_applications",
            p,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<(string Status, string Message, int ApplicationId)> CreateAsync(CreditApplication application)
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_customer_id", application.CustomerId, OracleMappingType.Int32, ParameterDirection.Input);
        p.Add("p_amount", application.Amount, OracleMappingType.Decimal, ParameterDirection.Input);
        p.Add("p_tenor_months", application.TenorMonths, OracleMappingType.Int32, ParameterDirection.Input);
        p.Add("out_id", dbType: OracleMappingType.Int32, direction: ParameterDirection.Output);
        p.Add("out_stat", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 50);
        p.Add("out_mess", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 500);

        await conn.ExecuteAsync(
            $"{Schema}.sp_create_application",
            p,
            commandType: CommandType.StoredProcedure);

        var status = p.Get<string>("out_stat") ?? "ERROR";
        var message = p.Get<string>("out_mess") ?? string.Empty;
        var id = p.Get<int>("out_id");

        if (status == "SUCCESS")
            application.ApplicationId = id;

        return (status, message, id);
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
            $"{Schema}.sp_approve_application_v2",
            p,
            commandType: CommandType.StoredProcedure);

        var status = p.Get<string>("out_stat") ?? "ERROR";
        var message = p.Get<string>("out_mess") ?? string.Empty;

        return (status, message);
    }
}
