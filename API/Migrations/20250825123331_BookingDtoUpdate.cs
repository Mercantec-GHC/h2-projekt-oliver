using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    public partial class BookingDtoUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE bookings
                  ALTER COLUMN check_in  TYPE timestamptz USING check_in  AT TIME ZONE 'UTC',
                  ALTER COLUMN check_out TYPE timestamptz USING check_out AT TIME ZONE 'UTC';
            ");

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
            // Reverse: convert timestamptz back to timestamp without time zone (treat stored values as UTC)
            migrationBuilder.Sql(@"
                ALTER TABLE bookings
                  ALTER COLUMN check_in  TYPE timestamp without time zone USING check_in  AT TIME ZONE 'UTC',
                  ALTER COLUMN check_out TYPE timestamp without time zone USING check_out AT TIME ZONE 'UTC';
            ");

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
