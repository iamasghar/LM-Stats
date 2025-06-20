﻿// <auto-generated />
using System;
using LM.Stats.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace LM.Stats.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("LM.Stats.Data.Models.Config", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Configs");
                });

            modelBuilder.Entity("LM.Stats.Data.Models.Hunt", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("FirstHuntTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("GoalPercentageHunt")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GoalPercentagePurchase")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("HuntCount")
                        .HasColumnType("int");

                    b.Property<int>("L1Hunt")
                        .HasColumnType("int");

                    b.Property<int>("L1Purchase")
                        .HasColumnType("int");

                    b.Property<int>("L2Hunt")
                        .HasColumnType("int");

                    b.Property<int>("L2Purchase")
                        .HasColumnType("int");

                    b.Property<int>("L3Hunt")
                        .HasColumnType("int");

                    b.Property<int>("L3Purchase")
                        .HasColumnType("int");

                    b.Property<int>("L4Hunt")
                        .HasColumnType("int");

                    b.Property<int>("L4Purchase")
                        .HasColumnType("int");

                    b.Property<int>("L5Hunt")
                        .HasColumnType("int");

                    b.Property<int>("L5Purchase")
                        .HasColumnType("int");

                    b.Property<DateTime>("LastHuntTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("PointsHunt")
                        .HasColumnType("int");

                    b.Property<int>("PointsPurchase")
                        .HasColumnType("int");

                    b.Property<int>("Purchase")
                        .HasColumnType("int");

                    b.Property<int?>("StatsId")
                        .HasColumnType("int");

                    b.Property<int>("Total")
                        .HasColumnType("int");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("StatsId");

                    b.ToTable("Hunts");
                });

            modelBuilder.Entity("LM.Stats.Data.Models.Kill", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<long>("IggId")
                        .HasColumnType("bigint");

                    b.Property<long>("Kills")
                        .HasColumnType("bigint");

                    b.Property<long>("KillsDifference")
                        .HasColumnType("bigint");

                    b.Property<decimal>("Might")
                        .HasColumnType("decimal(20,0)");

                    b.Property<long>("MightDifference")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("OldKills")
                        .HasColumnType("bigint");

                    b.Property<decimal>("OldMight")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("OldName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Rank")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("StatsId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("StatsId");

                    b.ToTable("Kills");
                });

            modelBuilder.Entity("LM.Stats.Data.Models.OtherStat", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<long>("EnemiesDestroyedMight")
                        .HasColumnType("bigint");

                    b.Property<long>("Kills")
                        .HasColumnType("bigint");

                    b.Property<long>("Might")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("StatsId")
                        .HasColumnType("int");

                    b.Property<long>("TroopsKilled")
                        .HasColumnType("bigint");

                    b.Property<long>("TroopsLost")
                        .HasColumnType("bigint");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<string>("WinRate")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("StatsId");

                    b.ToTable("OtherStats");
                });

            modelBuilder.Entity("LM.Stats.Data.Models.StatsInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("FromDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ToDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("UniqueIdentifier")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Stats");
                });

            modelBuilder.Entity("LM.Stats.Data.Models.StatsSummary", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<long>("EDM")
                        .HasColumnType("bigint");

                    b.Property<long>("EDMDifference")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("FirstHuntTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("HuntPercentage")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("HuntPoints")
                        .HasColumnType("int");

                    b.Property<long>("Kills")
                        .HasColumnType("bigint");

                    b.Property<long>("KillsDifference")
                        .HasColumnType("bigint");

                    b.Property<decimal>("KillsPercentage")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("LastHuntTime")
                        .HasColumnType("datetime2");

                    b.Property<long>("Might")
                        .HasColumnType("bigint");

                    b.Property<long>("MightDifference")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("PurchasePercentage")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("PurchasePoints")
                        .HasColumnType("int");

                    b.Property<string>("Rank")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("StatsId")
                        .HasColumnType("int");

                    b.Property<long>("TroopsLost")
                        .HasColumnType("bigint");

                    b.Property<long>("TroopsLostDifference")
                        .HasColumnType("bigint");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<string>("Zone")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("StatsId");

                    b.ToTable("StatsSummaries");
                });

            modelBuilder.Entity("LM.Stats.Data.Models.Hunt", b =>
                {
                    b.HasOne("LM.Stats.Data.Models.StatsInfo", "Stats")
                        .WithMany("Hunts")
                        .HasForeignKey("StatsId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Stats");
                });

            modelBuilder.Entity("LM.Stats.Data.Models.Kill", b =>
                {
                    b.HasOne("LM.Stats.Data.Models.StatsInfo", "Stats")
                        .WithMany("Kills")
                        .HasForeignKey("StatsId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Stats");
                });

            modelBuilder.Entity("LM.Stats.Data.Models.OtherStat", b =>
                {
                    b.HasOne("LM.Stats.Data.Models.StatsInfo", "Stats")
                        .WithMany("OtherStats")
                        .HasForeignKey("StatsId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Stats");
                });

            modelBuilder.Entity("LM.Stats.Data.Models.StatsSummary", b =>
                {
                    b.HasOne("LM.Stats.Data.Models.StatsInfo", "Stats")
                        .WithMany()
                        .HasForeignKey("StatsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Stats");
                });

            modelBuilder.Entity("LM.Stats.Data.Models.StatsInfo", b =>
                {
                    b.Navigation("Hunts");

                    b.Navigation("Kills");

                    b.Navigation("OtherStats");
                });
#pragma warning restore 612, 618
        }
    }
}
