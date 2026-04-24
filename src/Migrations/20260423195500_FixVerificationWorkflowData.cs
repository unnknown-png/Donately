using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Donately.Migrations;

public partial class FixVerificationWorkflowData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"UPDATE ""VerificationRequests""
SET ""Status"" = 'InProgress'
WHERE ""EmailConfirmedAt"" IS NOT NULL
  AND ""Status"" <> 'InProgress';");

        migrationBuilder.Sql(@"UPDATE ""Users"" u
SET ""VerificationStatus"" = 1
FROM ""VerificationRequests"" vr
WHERE vr.""UserId"" = u.""Id""
  AND vr.""EmailConfirmedAt"" IS NOT NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"UPDATE ""VerificationRequests""
SET ""Status"" = 'InReview'
WHERE ""EmailConfirmedAt"" IS NOT NULL
  AND ""Status"" = 'InProgress';");

        migrationBuilder.Sql(@"UPDATE ""Users"" u
SET ""VerificationStatus"" = 0
FROM ""VerificationRequests"" vr
WHERE vr.""UserId"" = u.""Id""
  AND vr.""EmailConfirmedAt"" IS NOT NULL;");
    }
}



