using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    public partial class DtoFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update admin password
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "HashedPassword",
                value: "$2a$11$C2sHsoVgVdP2rzn93K9c2O8u9i4cVtFjYJya0w1PKgJjLgM9bIr96");

            // Convert bookings check_in/check_out to timestamptz (treat existing values as UTC)
            migrationBuilder.Sql(@"
                ALTER TABLE bookings
                  ALTER COLUMN check_in  TYPE timestamptz USING check_in  AT TIME ZONE 'UTC',
                  ALTER COLUMN check_out TYPE timestamptz USING check_out AT TIME ZONE 'UTC';
            ");

            // Convert audit columns in other tables to timestamptz
            migrationBuilder.Sql(@"
                ALTER TABLE rooms
                  ALTER COLUMN ""CreatedAt"" TYPE timestamptz USING ""CreatedAt"" AT TIME ZONE 'UTC',
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamptz USING ""UpdatedAt"" AT TIME ZONE 'UTC';
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE roles
                  ALTER COLUMN ""CreatedAt"" TYPE timestamptz USING ""CreatedAt"" AT TIME ZONE 'UTC',
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamptz USING ""UpdatedAt"" AT TIME ZONE 'UTC';
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE users
                  ALTER COLUMN ""CreatedAt"" TYPE timestamptz USING ""CreatedAt"" AT TIME ZONE 'UTC',
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamptz USING ""UpdatedAt"" AT TIME ZONE 'UTC',
                  ALTER COLUMN ""LastLogin"" TYPE timestamptz USING ""LastLogin"" AT TIME ZONE 'UTC';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reset admin password
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "HashedPassword",
                value: "$2a$11$VkHJzIzfLRlflsxWdFKsTOpDfGUrvwG5gmbvcFw7WxnYGuSp19RNC");

            // Revert bookings check_in/check_out back to timestamp without time zone
            migrationBuilder.Sql(@"
                ALTER TABLE bookings
                  ALTER COLUMN check_in  TYPE timestamp without time zone USING check_in AT TIME ZONE 'UTC',
                  ALTER COLUMN check_out TYPE timestamp without time zone USING check_out AT TIME ZONE 'UTC';
            ");

            // Revert audit columns
            migrationBuilder.Sql(@"
                ALTER TABLE rooms
                  ALTER COLUMN ""CreatedAt"" TYPE timestamp without time zone USING ""CreatedAt"" AT TIME ZONE 'UTC',
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamp without time zone USING ""UpdatedAt"" AT TIME ZONE 'UTC';
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE roles
                  ALTER COLUMN ""CreatedAt"" TYPE timestamp without time zone USING ""CreatedAt"" AT TIME ZONE 'UTC',
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamp without time zone USING ""UpdatedAt"" AT TIME ZONE 'UTC';
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE users
                  ALTER COLUMN ""CreatedAt"" TYPE timestamp without time zone USING ""CreatedAt"" AT TIME ZONE 'UTC',
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamp without time zone USING ""UpdatedAt"" AT TIME ZONE 'UTC',
                  ALTER COLUMN ""LastLogin"" TYPE timestamp without time zone USING ""LastLogin"" AT TIME ZONE 'UTC';
            ");
        }
    }
}
