using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GS.TenantService.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    UsesDedicatedDatabase = table.Column<bool>(type: "boolean", nullable: false),
                    DatabaseHost = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DatabasePort = table.Column<int>(type: "integer", nullable: true),
                    CredentialsRef = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantCode",
                table: "Tenants",
                column: "TenantCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
