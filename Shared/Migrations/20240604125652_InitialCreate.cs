using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalutationCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StreetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StreetNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Town = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PackstationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PostNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PostOfficeNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CountryIsoCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryAddress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SalutationCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StreetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StreetNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Town = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PackstationNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PostNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PostOfficeNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CountryIsoCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAddress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AldiCustomerNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    OrderDeliveryArea = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    Exported = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.Id);
                    table.UniqueConstraint("AK_Order_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ShippingAddress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SalutationCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StreetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StreetNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Town = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PackstationNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PostNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PostOfficeNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CountryIsoCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingAddress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerInfo_Address_AddressId",
                        column: x => x.AddressId,
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    EntryNumber = table.Column<int>(type: "int", nullable: false),
                    VendorProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AldiProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CanceledOrReturnedQuantity = table.Column<int>(type: "int", nullable: false),
                    CarrierCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AldiSuedProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeliveryAddressId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderEntry_DeliveryAddress_DeliveryAddressId",
                        column: x => x.DeliveryAddressId,
                        principalTable: "DeliveryAddress",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderEntry_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorConsignmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatusText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrackingId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrackingLink = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Carrier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShippingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpectedDelivery = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceiptDelivery = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AldiConsignmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ShippingAddressId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consignment_Order_OrderCode",
                        column: x => x.OrderCode,
                        principalTable: "Order",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Consignment_ShippingAddress_ShippingAddressId",
                        column: x => x.ShippingAddressId,
                        principalTable: "ShippingAddress",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Return",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InitiationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AldiReturnCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rma = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerInfoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Return", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Return_CustomerInfo_CustomerInfoId",
                        column: x => x.CustomerInfoId,
                        principalTable: "CustomerInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Return_Order_OrderCode",
                        column: x => x.OrderCode,
                        principalTable: "Order",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsignmentEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsignmentId = table.Column<int>(type: "int", nullable: false),
                    OrderEntryNumber = table.Column<int>(type: "int", nullable: false),
                    OrderEntryId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CancelledOrReturnedQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsignmentEntry_Consignment_ConsignmentId",
                        column: x => x.ConsignmentId,
                        principalTable: "Consignment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsignmentEntry_OrderEntry_OrderEntryId",
                        column: x => x.OrderEntryId,
                        principalTable: "OrderEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReturnEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrderEntryNumber = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CanceledOrReturnedQuantity = table.Column<int>(type: "int", nullable: false),
                    EntryCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CarrierCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConsignmentEntryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnEntry_ConsignmentEntry_ConsignmentEntryId",
                        column: x => x.ConsignmentEntryId,
                        principalTable: "ConsignmentEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReturnEntry_Return_ReturnId",
                        column: x => x.ReturnId,
                        principalTable: "Return",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReturnConsignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsignmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CanceledQuantity = table.Column<int>(type: "int", nullable: false),
                    CompletedQuantity = table.Column<int>(type: "int", nullable: false),
                    Carrier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CarrierCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnEntryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnConsignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnConsignment_ReturnEntry_ReturnEntryId",
                        column: x => x.ReturnEntryId,
                        principalTable: "ReturnEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReturnPackage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorPackageCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrackingId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrackingLink = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceiptDelivery = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnConsignmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnPackage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnPackage_ReturnConsignment_ReturnConsignmentId",
                        column: x => x.ReturnConsignmentId,
                        principalTable: "ReturnConsignment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consignment_OrderCode",
                table: "Consignment",
                column: "OrderCode");

            migrationBuilder.CreateIndex(
                name: "IX_Consignment_ShippingAddressId",
                table: "Consignment",
                column: "ShippingAddressId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentEntry_ConsignmentId",
                table: "ConsignmentEntry",
                column: "ConsignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentEntry_OrderEntryId",
                table: "ConsignmentEntry",
                column: "OrderEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInfo_AddressId",
                table: "CustomerInfo",
                column: "AddressId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Order_Code",
                table: "Order",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderEntry_DeliveryAddressId",
                table: "OrderEntry",
                column: "DeliveryAddressId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderEntry_OrderId",
                table: "OrderEntry",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Return_CustomerInfoId",
                table: "Return",
                column: "CustomerInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Return_OrderCode",
                table: "Return",
                column: "OrderCode");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnConsignment_ReturnEntryId",
                table: "ReturnConsignment",
                column: "ReturnEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnEntry_ConsignmentEntryId",
                table: "ReturnEntry",
                column: "ConsignmentEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnEntry_ReturnId",
                table: "ReturnEntry",
                column: "ReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnPackage_ReturnConsignmentId",
                table: "ReturnPackage",
                column: "ReturnConsignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessToken");

            migrationBuilder.DropTable(
                name: "ReturnPackage");

            migrationBuilder.DropTable(
                name: "ReturnConsignment");

            migrationBuilder.DropTable(
                name: "ReturnEntry");

            migrationBuilder.DropTable(
                name: "ConsignmentEntry");

            migrationBuilder.DropTable(
                name: "Return");

            migrationBuilder.DropTable(
                name: "Consignment");

            migrationBuilder.DropTable(
                name: "OrderEntry");

            migrationBuilder.DropTable(
                name: "CustomerInfo");

            migrationBuilder.DropTable(
                name: "ShippingAddress");

            migrationBuilder.DropTable(
                name: "DeliveryAddress");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "Address");
        }
    }
}
