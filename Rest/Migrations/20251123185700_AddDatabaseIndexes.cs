using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rest.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_applicationuser_email",
                table: "AspNetUsers",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_applicationuser_username",
                table: "AspNetUsers",
                column: "user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_applicationuser_lastlogin",
                table: "AspNetUsers",
                column: "last_login_at");

            migrationBuilder.CreateIndex(
                name: "idx_refreshtoken_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_refreshtoken_expires",
                table: "refresh_tokens",
                column: "expires");

            migrationBuilder.CreateIndex(
                name: "idx_refreshtoken_revoked",
                table: "refresh_tokens",
                column: "revoked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_applicationuser_email",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "idx_applicationuser_username",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "idx_applicationuser_lastlogin",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "idx_refreshtoken_token",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "idx_refreshtoken_expires",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "idx_refreshtoken_revoked",
                table: "refresh_tokens");
        }
    }
}
