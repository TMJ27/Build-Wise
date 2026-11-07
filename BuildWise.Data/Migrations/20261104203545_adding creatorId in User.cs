using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingcreatorIdinUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoStorageKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DefaultCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    DefaultQuotationTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultValidityDays = table.Column<int>(type: "int", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Client",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerTypeId = table.Column<int>(type: "int", nullable: false),
                    NotesJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiteAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PropertyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientRequest_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Currency",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Currency_CreationDate"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currency", x => x.Id);
                    table.UniqueConstraint("UQ_Currency_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Quotation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotation_ClientRequest",
                        column: x => x.RequestId,
                        principalTable: "ClientRequest",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuotationItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    UnitCodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequiresSiteAssessment = table.Column<bool>(type: "bit", nullable: false),
                    AssessmentNotesSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WorkStagesSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    DependenciesSnapshotJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationItem_ClientRequest",
                        column: x => x.RequestId,
                        principalTable: "ClientRequest",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuotationRevision",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CompanyNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CompanyEmailSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CompanyPhoneSnapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CompanyAddressSnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyRegistrationNumberSnapshot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CompanyTaxNumberSnapshot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CompanyLogoStorageKeySnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CustomerEmailSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CustomerPhoneSnapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CustomerAddressSnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SiteAddressSnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestDescriptionSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClarificationsSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Terms = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Exclusions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IssueDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime", nullable: true),
                    IncludesPrices = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_QuotationRevision_IncludesPrices"),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    TaxTreatmentId = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                        .Annotation("Relational:DefaultConstraintName", "DF_QuotationRevision_TaxTreatment"),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(sysutcdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_QuotationRevision_CreatedAtUtc"),
                    LastUpdate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ApprovedDocumentJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationRevision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationRevision_Quotation",
                        column: x => x.QuotationId,
                        principalTable: "Quotation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScopeClause",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClauseTypeId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeClause", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScopeDependency",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependsOnScopeItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotesJson = table.Column<string>(type: "text", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeDependency", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScopeDependency_ClientRequest",
                        column: x => x.RequestId,
                        principalTable: "ClientRequest",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScopeItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    UnitCodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequireAssessment = table.Column<bool>(type: "bit", nullable: false),
                    AssessmentNotesJson = table.Column<string>(type: "text", nullable: true),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    ReviewStatusId = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedOn = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScopeItem_ClientRequest",
                        column: x => x.RequestId,
                        principalTable: "ClientRequest",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScopeStage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeStage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScopeStage_ScopeItem",
                        column: x => x.ScopeItemId,
                        principalTable: "ScopeItem",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequireSiteAssessment = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Service", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Service_ServiceCategory",
                        column: x => x.CategoryId,
                        principalTable: "ServiceCategory",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceClause",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClauseTypeId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceClause", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceClause_Service",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceDependency",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependsOnServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDependency", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDependency_Service",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceDependency_Service1",
                        column: x => x.DependsOnServiceId,
                        principalTable: "Service",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceStage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Desrciption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceStage_Service",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceUnit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceUnit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceUnit_Service",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Unit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AuthenticationKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AuthRecoveryCodes = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TwoFactorAuthEnabled = table.Column<bool>(type: "bit", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: true),
                    ActivationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RightsId = table.Column<int>(type: "int", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroup_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSession",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LockoutDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSession", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserSession_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Client_CreatorId",
                table: "Client",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientRequest_ClientId",
                table: "ClientRequest",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientRequest_ConfirmedBy",
                table: "ClientRequest",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ClientRequest_CreatorId",
                table: "ClientRequest",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_CreatorId",
                table: "Currency",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_UserId",
                table: "Currency",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_CreatorId",
                table: "Quotation",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_RequestId",
                table: "Quotation",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItem_QuotationRevisionId",
                table: "QuotationItem",
                column: "QuotationRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItem_RequestId",
                table: "QuotationItem",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItem_ScopeItemId",
                table: "QuotationItem",
                column: "ScopeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItem_ServiceId",
                table: "QuotationItem",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationRevision_ApprovedByUserId",
                table: "QuotationRevision",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationRevision_CreatedByUserId",
                table: "QuotationRevision",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "UQ_QuotationRevision_Number",
                table: "QuotationRevision",
                columns: new[] { "QuotationId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScopeClause_CreatorId",
                table: "ScopeClause",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeClause_ScopeItemId",
                table: "ScopeClause",
                column: "ScopeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeDependency_CreatorId",
                table: "ScopeDependency",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeDependency_DependsOnScopeItemId",
                table: "ScopeDependency",
                column: "DependsOnScopeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeDependency_RequestId",
                table: "ScopeDependency",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeDependency_ScopeItemId",
                table: "ScopeDependency",
                column: "ScopeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeItem_RequestId",
                table: "ScopeItem",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeItem_ReviewedByUserId",
                table: "ScopeItem",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeItem_ServiceId",
                table: "ScopeItem",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeStage_CreatorId",
                table: "ScopeStage",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeStage_ScopeItemId",
                table: "ScopeStage",
                column: "ScopeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Service_CategoryId",
                table: "Service",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Service_CreatorId",
                table: "Service",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceClause_ServiceId",
                table: "ServiceClause",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDependency_CreatorId",
                table: "ServiceDependency",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDependency_DependsOnServiceId",
                table: "ServiceDependency",
                column: "DependsOnServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDependency_ServiceId",
                table: "ServiceDependency",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStage_CreatorId",
                table: "ServiceStage",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStage_ServiceId",
                table: "ServiceStage",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUnit_ServiceId",
                table: "ServiceUnit",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUnit_UnitId",
                table: "ServiceUnit",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Unit_CreatorId",
                table: "Unit",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_User_CreatorId",
                table: "User",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_User_GroupId",
                table: "User",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroup_CreatorId",
                table: "UserGroup",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_User",
                table: "Client",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientRequest_User",
                table: "ClientRequest",
                column: "ConfirmedBy",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientRequest_User1",
                table: "ClientRequest",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Currency_User",
                table: "Currency",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Currency_User_UserId",
                table: "Currency",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotation_User",
                table: "Quotation",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationItem_QuotationRevision",
                table: "QuotationItem",
                column: "QuotationRevisionId",
                principalTable: "QuotationRevision",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationItem_ScopeItem",
                table: "QuotationItem",
                column: "ScopeItemId",
                principalTable: "ScopeItem",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationItem_Service",
                table: "QuotationItem",
                column: "ServiceId",
                principalTable: "Service",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationRevision_ApprovedByUser",
                table: "QuotationRevision",
                column: "ApprovedByUserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationRevision_User",
                table: "QuotationRevision",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScopeClause_ScopeItem",
                table: "ScopeClause",
                column: "ScopeItemId",
                principalTable: "ScopeItem",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScopeClause_User",
                table: "ScopeClause",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScopeDependency_ScopeItem",
                table: "ScopeDependency",
                column: "ScopeItemId",
                principalTable: "ScopeItem",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScopeDependency_ScopeItem1",
                table: "ScopeDependency",
                column: "DependsOnScopeItemId",
                principalTable: "ScopeItem",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScopeDependency_User",
                table: "ScopeDependency",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScopeItem_Service",
                table: "ScopeItem",
                column: "ServiceId",
                principalTable: "Service",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScopeItem_User",
                table: "ScopeItem",
                column: "ReviewedByUserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScopeStage_User",
                table: "ScopeStage",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Service_User",
                table: "Service",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceDependency_User",
                table: "ServiceDependency",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceStage_User",
                table: "ServiceStage",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceUnit_Unit",
                table: "ServiceUnit",
                column: "UnitId",
                principalTable: "Unit",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Unit_User",
                table: "Unit",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_User_UserGroup",
                table: "User",
                column: "GroupId",
                principalTable: "UserGroup",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserGroup_User",
                table: "UserGroup");

            migrationBuilder.DropTable(
                name: "Company");

            migrationBuilder.DropTable(
                name: "Currency");

            migrationBuilder.DropTable(
                name: "QuotationItem");

            migrationBuilder.DropTable(
                name: "ScopeClause");

            migrationBuilder.DropTable(
                name: "ScopeDependency");

            migrationBuilder.DropTable(
                name: "ScopeStage");

            migrationBuilder.DropTable(
                name: "ServiceClause");

            migrationBuilder.DropTable(
                name: "ServiceDependency");

            migrationBuilder.DropTable(
                name: "ServiceStage");

            migrationBuilder.DropTable(
                name: "ServiceUnit");

            migrationBuilder.DropTable(
                name: "UserSession");

            migrationBuilder.DropTable(
                name: "QuotationRevision");

            migrationBuilder.DropTable(
                name: "ScopeItem");

            migrationBuilder.DropTable(
                name: "Unit");

            migrationBuilder.DropTable(
                name: "Quotation");

            migrationBuilder.DropTable(
                name: "Service");

            migrationBuilder.DropTable(
                name: "ClientRequest");

            migrationBuilder.DropTable(
                name: "ServiceCategory");

            migrationBuilder.DropTable(
                name: "Client");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "UserGroup");
        }
    }
}
