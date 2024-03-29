﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using raidTimeline.Database.Context;

#nullable disable

namespace raidTimeline.Database.Migrations
{
    [DbContext(typeof(RaidContext))]
    [Migration("20220227130039_Migration00")]
    partial class Migration00
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("EncounterPlayer", b =>
                {
                    b.Property<Guid>("EncountersId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("PlayersId")
                        .HasColumnType("uuid");

                    b.HasKey("EncountersId", "PlayersId");

                    b.HasIndex("PlayersId");

                    b.ToTable("EncounterPlayer", (string)null);
                });

            modelBuilder.Entity("raidTimeline.Database.Models.Boss", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("FightId")
                        .HasColumnType("integer");

                    b.Property<string>("Icon")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Bosses", (string)null);
                });

            modelBuilder.Entity("raidTimeline.Database.Models.Encounter", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("BossForeignKey")
                        .HasColumnType("uuid");

                    b.Property<string>("EncounterTime")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double>("HitPointsRemaining")
                        .HasColumnType("double precision");

                    b.Property<bool>("Killed")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("OccurenceEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("OccurenceStart")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("BossForeignKey");

                    b.ToTable("Encounters", (string)null);
                });

            modelBuilder.Entity("raidTimeline.Database.Models.GeneralStatistics", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<long>("Cc")
                        .HasColumnType("bigint");

                    b.Property<long>("Damage")
                        .HasColumnType("bigint");

                    b.Property<long>("Dps")
                        .HasColumnType("bigint");

                    b.Property<Guid>("EncounterId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("PlayerId")
                        .HasColumnType("uuid");

                    b.Property<int>("ResAmount")
                        .HasColumnType("integer");

                    b.Property<double>("ResTime")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.HasIndex("EncounterId");

                    b.HasIndex("PlayerId");

                    b.ToTable("GeneralStatistics", (string)null);
                });

            modelBuilder.Entity("raidTimeline.Database.Models.Player", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AccountName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Players", (string)null);
                });

            modelBuilder.Entity("EncounterPlayer", b =>
                {
                    b.HasOne("raidTimeline.Database.Models.Encounter", null)
                        .WithMany()
                        .HasForeignKey("EncountersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("raidTimeline.Database.Models.Player", null)
                        .WithMany()
                        .HasForeignKey("PlayersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("raidTimeline.Database.Models.Encounter", b =>
                {
                    b.HasOne("raidTimeline.Database.Models.Boss", "Boss")
                        .WithMany("Encounters")
                        .HasForeignKey("BossForeignKey")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Boss");
                });

            modelBuilder.Entity("raidTimeline.Database.Models.GeneralStatistics", b =>
                {
                    b.HasOne("raidTimeline.Database.Models.Encounter", "Encounter")
                        .WithMany()
                        .HasForeignKey("EncounterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("raidTimeline.Database.Models.Player", "Player")
                        .WithMany()
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Encounter");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("raidTimeline.Database.Models.Boss", b =>
                {
                    b.Navigation("Encounters");
                });
#pragma warning restore 612, 618
        }
    }
}
