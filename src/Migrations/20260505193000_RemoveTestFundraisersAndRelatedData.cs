using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Donately.Migrations;

public partial class RemoveTestFundraisersAndRelatedData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TEMP TABLE temp_target_fundraisers ON COMMIT DROP AS
            SELECT "Id"
            FROM "Fundraisers"
            WHERE "Id" IN (
                'db4d94fe-da84-4dc4-acee-31297b7eb34c',
                '01cef099-0e8a-4963-b175-e996e5da3107',
                '8d0264d6-4b01-4019-aaa3-a0a77613c4b3',
                '60c1b989-184c-4c74-b361-96734a9a1ca7'
            );

            CREATE TEMP TABLE temp_target_donations ON COMMIT DROP AS
            SELECT "Id", "PaymentTransactionId"
            FROM "Donations"
            WHERE "FundraiserId" IN (SELECT "Id" FROM temp_target_fundraisers);

            CREATE TEMP TABLE temp_target_payment_transactions ON COMMIT DROP AS
            SELECT DISTINCT pt."Id"
            FROM "PaymentTransactions" pt
            WHERE pt."Id" IN (
                SELECT "PaymentTransactionId"
                FROM temp_target_donations
                WHERE "PaymentTransactionId" IS NOT NULL
            );

            CREATE TEMP TABLE temp_target_provider_transaction_ids ON COMMIT DROP AS
            SELECT DISTINCT pt."ProviderTransactionId" AS "ProviderTransactionId"
            FROM "PaymentTransactions" pt
            WHERE pt."Id" IN (SELECT "Id" FROM temp_target_payment_transactions)
              AND pt."ProviderTransactionId" IS NOT NULL;

            CREATE TEMP TABLE temp_target_order_ids ON COMMIT DROP AS
            SELECT DISTINCT order_id AS "OrderId"
            FROM (
                SELECT pt."Metadata"->>'order_id' AS order_id
                FROM "PaymentTransactions" pt
                WHERE pt."Id" IN (SELECT "Id" FROM temp_target_payment_transactions)
                  AND pt."Metadata" IS NOT NULL

                UNION ALL

                SELECT pt."Metadata"->>'liqpay_order_id' AS order_id
                FROM "PaymentTransactions" pt
                WHERE pt."Id" IN (SELECT "Id" FROM temp_target_payment_transactions)
                  AND pt."Metadata" IS NOT NULL
            ) target_orders
            WHERE order_id IS NOT NULL AND order_id <> '';

            CREATE TEMP TABLE temp_target_webhook_events ON COMMIT DROP AS
            SELECT "Id"
            FROM "WebhookEvents"
            WHERE COALESCE("Payload"->>'order_id', '') IN (SELECT "OrderId" FROM temp_target_order_ids)
               OR COALESCE("Payload"->>'liqpay_order_id', '') IN (SELECT "OrderId" FROM temp_target_order_ids)
               OR COALESCE("Payload"->>'payment_id', '') IN (SELECT "ProviderTransactionId" FROM temp_target_provider_transaction_ids)
               OR COALESCE("Payload"->>'transaction_id', '') IN (SELECT "ProviderTransactionId" FROM temp_target_provider_transaction_ids);

            CREATE TEMP TABLE temp_target_attachments ON COMMIT DROP AS
            SELECT "Id"
            FROM "Attachments"
            WHERE "OwnerType" = 'Fundraiser'
              AND "OwnerId" IN (SELECT "Id" FROM temp_target_fundraisers);

            CREATE TEMP TABLE temp_target_audit_logs ON COMMIT DROP AS
            SELECT "Id"
            FROM "AuditLogs"
            WHERE ("EntityType" = 'Fundraiser' AND "EntityId" IN (SELECT "Id" FROM temp_target_fundraisers))
               OR ("EntityType" = 'Donation' AND "EntityId" IN (SELECT "Id" FROM temp_target_donations))
               OR ("EntityType" = 'PaymentTransaction' AND "EntityId" IN (SELECT "Id" FROM temp_target_payment_transactions));

            DELETE FROM "Donations"
            WHERE "Id" IN (SELECT "Id" FROM temp_target_donations);

            DELETE FROM "PaymentTransactions"
            WHERE "Id" IN (SELECT "Id" FROM temp_target_payment_transactions);

            DELETE FROM "WebhookEvents"
            WHERE "Id" IN (SELECT "Id" FROM temp_target_webhook_events);

            DELETE FROM "Attachments"
            WHERE "Id" IN (SELECT "Id" FROM temp_target_attachments);

            DELETE FROM "AuditLogs"
            WHERE "Id" IN (SELECT "Id" FROM temp_target_audit_logs);

            DELETE FROM "Fundraisers"
            WHERE "Id" IN (SELECT "Id" FROM temp_target_fundraisers);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Irreversible data cleanup migration.
    }
}


