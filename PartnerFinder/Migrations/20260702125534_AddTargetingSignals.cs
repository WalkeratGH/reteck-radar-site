using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerFinder.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetingSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EquipmentLeasingSignal",
                table: "Partners",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmeFocusSignal",
                table: "Partners",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EquipmentLeasingSignal",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "SmeFocusSignal",
                table: "Partners");
        }
    }
}
