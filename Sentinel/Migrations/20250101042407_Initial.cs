using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppBinaryBaseDirs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBinaryBaseDirs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppBinaries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Guid = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppBinaryBaseDirId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBinaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppBinaries_AppBinaryBaseDirs_AppBinaryBaseDirId",
                        column: x => x.AppBinaryBaseDirId,
                        principalTable: "AppBinaryBaseDirs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppBinaryFile",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false),
                    LastWriteUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AppBinaryId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBinaryFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppBinaryFile_AppBinaries_AppBinaryId",
                        column: x => x.AppBinaryId,
                        principalTable: "AppBinaries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppBinaryFileChunk",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OffsetBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false),
                    AppBinaryFileId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBinaryFileChunk", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppBinaryFileChunk_AppBinaryFile_AppBinaryFileId",
                        column: x => x.AppBinaryFileId,
                        principalTable: "AppBinaryFile",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppBinaries_AppBinaryBaseDirId",
                table: "AppBinaries",
                column: "AppBinaryBaseDirId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBinaries_Guid",
                table: "AppBinaries",
                column: "Guid");

            migrationBuilder.CreateIndex(
                name: "IX_AppBinaries_Path",
                table: "AppBinaries",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_AppBinaryBaseDirs_Name",
                table: "AppBinaryBaseDirs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppBinaryFile_AppBinaryId",
                table: "AppBinaryFile",
                column: "AppBinaryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBinaryFile_FilePath",
                table: "AppBinaryFile",
                column: "FilePath");

            migrationBuilder.CreateIndex(
                name: "IX_AppBinaryFileChunk_AppBinaryFileId",
                table: "AppBinaryFileChunk",
                column: "AppBinaryFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBinaryFileChunk");

            migrationBuilder.DropTable(
                name: "AppBinaryFile");

            migrationBuilder.DropTable(
                name: "AppBinaries");

            migrationBuilder.DropTable(
                name: "AppBinaryBaseDirs");
        }
    }
}
