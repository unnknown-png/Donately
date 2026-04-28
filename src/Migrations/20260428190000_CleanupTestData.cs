using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Donately.Migrations;

public partial class CleanupTestData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"CREATE TEMP TABLE temp_target_users ON COMMIT DROP AS
SELECT ""Id""
FROM ""Users""
WHERE ""FullName"" IN ('Adrian', 'Solia Chvartkovska', 'test me');");

        migrationBuilder.Sql(@"CREATE TEMP TABLE temp_target_fundraisers ON COMMIT DROP AS
SELECT ""Id""
FROM ""Fundraisers""
WHERE ""CreatedById"" IN (SELECT ""Id"" FROM temp_target_users);");

        migrationBuilder.Sql(@"CREATE TEMP TABLE temp_target_payment_transactions ON COMMIT DROP AS
SELECT ""Id""
FROM ""PaymentTransactions""
WHERE ""Id"" IN (
    'aa9ef886-1985-4456-98f6-2ff7aeba32e9',
    '4e51fdac-5f14-4bd3-87ff-3ef5b650d5cd',
    '24a3da34-983d-4011-846a-673c087d1296',
    '9f5e9dc4-452e-4248-a81c-69d1903836cc',
    '04a6e514-c37e-4d55-9f24-59eae7dee985',
    'a3004ac9-627e-4c68-af09-8ab9770fc774',
    'd69e8c71-06f3-494e-ab0c-4e19c3692a83',
    '9ef2a7c7-1e33-4577-885f-2eb8a2b4be11',
    '451e09e2-cb51-4554-88d5-b4a4b3dd3f94',
    '1be516c1-fdc2-4dd5-af84-9fe0a529b5d4',
    '306bf22a-d4a4-4c76-9726-479cf651c0e0',
    '5d838ca8-5b00-436e-b2c0-601c053f0842',
    '59e355a1-e9ef-4562-b084-693fdef0a3d8',
    '7da0d6d2-a312-4133-b960-7163e0e9527b',
    '4e82c15e-a26a-4221-8ec3-968e051352bc',
    '4aff3ad3-f885-4cbe-8718-1bd74394db61',
    '9963e0cd-cd5c-43e7-858d-f602763442c6',
    '16cd555e-25b5-46f6-a4c7-6024683e8b0b',
    '6d2f8a23-abb8-424d-8baa-15006550e6c1'
);");

        migrationBuilder.Sql(@"CREATE TEMP TABLE temp_target_verification_requests ON COMMIT DROP AS
SELECT ""Id""
FROM ""VerificationRequests""
WHERE ""UserId"" IN (SELECT ""Id"" FROM temp_target_users)
   OR ""ReviewedById"" IN (SELECT ""Id"" FROM temp_target_users)
   OR ""UserId"" = '3031ff60-9edb-43d3-a0b7-4dcc88a475d2';");

        migrationBuilder.Sql(@"CREATE TEMP TABLE temp_target_audit_logs ON COMMIT DROP AS
SELECT ""Id""
FROM ""AuditLogs""
WHERE ""PerformedById"" IN (SELECT ""Id"" FROM temp_target_users);");

        migrationBuilder.Sql(@"DELETE FROM ""Donations""
WHERE ""PaymentTransactionId"" IN (SELECT ""Id"" FROM temp_target_payment_transactions)
   OR ""FundraiserId"" IN (SELECT ""Id"" FROM temp_target_fundraisers);");

        migrationBuilder.Sql(@"DELETE FROM ""PaymentTransactions""
WHERE ""Id"" IN (SELECT ""Id"" FROM temp_target_payment_transactions);");

        migrationBuilder.Sql(@"DELETE FROM ""VerificationRequests""
WHERE ""Id"" IN (SELECT ""Id"" FROM temp_target_verification_requests);");

        migrationBuilder.Sql(@"DELETE FROM ""AuditLogs""
WHERE ""Id"" IN (SELECT ""Id"" FROM temp_target_audit_logs);");

        migrationBuilder.Sql(@"DELETE FROM ""Fundraisers""
WHERE ""Id"" IN (SELECT ""Id"" FROM temp_target_fundraisers);");

        migrationBuilder.Sql(@"DELETE FROM ""Users""
WHERE ""Id"" IN (SELECT ""Id"" FROM temp_target_users);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Irreversible data cleanup migration.
    }
}

