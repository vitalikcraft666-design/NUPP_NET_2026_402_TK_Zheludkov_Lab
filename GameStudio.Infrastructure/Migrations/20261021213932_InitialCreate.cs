using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HireDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BaseSalary = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.UniqueConstraint("AK_Employees_PublicId", x => x.PublicId);
                });

            migrationBuilder.CreateTable(
                name: "GameProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Genre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Engine = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Budget = table.Column<double>(type: "REAL", nullable: false),
                    Spent = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameProjects", x => x.Id);
                    table.UniqueConstraint("AK_GameProjects_PublicId", x => x.PublicId);
                });

            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Specialization = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    MainTool = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ArtworksCompleted = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Artists_Employees_Id",
                        column: x => x.Id,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Programmers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrimaryLanguage = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Grade = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CommitsCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programmers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Programmers_Employees_Id",
                        column: x => x.Id,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAssignments",
                columns: table => new
                {
                    GameProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAssignments", x => new { x.GameProjectId, x.EmployeeId });
                    table.ForeignKey(
                        name: "FK_ProjectAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectAssignments_GameProjects_GameProjectId",
                        column: x => x.GameProjectId,
                        principalTable: "GameProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StorePages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Price = table.Column<double>(type: "REAL", nullable: false),
                    WishlistCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePages", x => x.Id);
                    table.CheckConstraint("CK_StorePages_WishlistCount", "\"WishlistCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_StorePages_GameProjects_GameProjectId",
                        column: x => x.GameProjectId,
                        principalTable: "GameProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BugReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsFixed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GameProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssigneeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BugReports", x => x.Id);
                    table.UniqueConstraint("AK_BugReports_PublicId", x => x.PublicId);
                    table.ForeignKey(
                        name: "FK_BugReports_GameProjects_GameProjectId",
                        column: x => x.GameProjectId,
                        principalTable: "GameProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BugReports_Programmers_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Programmers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_AssigneeId",
                table: "BugReports",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_GameProjectId",
                table: "BugReports",
                column: "GameProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_Severity_IsFixed",
                table: "BugReports",
                columns: new[] { "Severity", "IsFixed" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FullName",
                table: "Employees",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_GameProjects_Status",
                table: "GameProjects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_EmployeeId",
                table: "ProjectAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePages_GameProjectId",
                table: "StorePages",
                column: "GameProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Artists");

            migrationBuilder.DropTable(
                name: "BugReports");

            migrationBuilder.DropTable(
                name: "ProjectAssignments");

            migrationBuilder.DropTable(
                name: "StorePages");

            migrationBuilder.DropTable(
                name: "Programmers");

            migrationBuilder.DropTable(
                name: "GameProjects");

            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
