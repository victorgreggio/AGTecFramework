using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGTec.Reference.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "reference");

            migrationBuilder.CreateTable(
                name: "forecasts",
                schema: "reference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    celsius = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forecasts", x => x.Id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "forecasts", schema: "reference");
        }
    }
}
