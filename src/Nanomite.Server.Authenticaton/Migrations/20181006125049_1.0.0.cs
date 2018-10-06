using Microsoft.EntityFrameworkCore.Migrations;

namespace Nanomite.Server.Authenticaton.Migrations
{
    public partial class _100 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NetworkUser",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CreatedDT = table.Column<string>(nullable: true),
                    ModifiedDT = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    LoginName = table.Column<string>(nullable: true),
                    PasswordHash = table.Column<string>(nullable: true),
                    AuthenticationToken = table.Column<string>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false),
                    IsAdmin = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkUser", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetworkUser");
        }
    }
}
