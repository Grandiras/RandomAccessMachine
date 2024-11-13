﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WebDataProject.Data.Contexts;

#nullable disable

namespace WebDataProject.Data.Migrations
{
    [DbContext(typeof(WebDataProjectContext))]
    [Migration("20240528161907_FixedCascadeAgain")]
    partial class FixedCascadeAgain
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("AGStudent", b =>
                {
                    b.Property<Guid>("AGsId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("MembersId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("AGsId", "MembersId");

                    b.HasIndex("MembersId");

                    b.ToTable("AGStudent");
                });

            modelBuilder.Entity("WebDataProject.Data.Models.AG", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("Day")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("AGs");
                });

            modelBuilder.Entity("WebDataProject.Data.Models.Class", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Classes");
                });

            modelBuilder.Entity("WebDataProject.Data.Models.Student", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Students");
                });

            modelBuilder.Entity("WebDataProject.Data.Models.Teacher", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Abbreviation")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Teachers");
                });

            modelBuilder.Entity("AGStudent", b =>
                {
                    b.HasOne("WebDataProject.Data.Models.AG", null)
                        .WithMany()
                        .HasForeignKey("AGsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("WebDataProject.Data.Models.Student", null)
                        .WithMany()
                        .HasForeignKey("MembersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("WebDataProject.Data.Models.AG", b =>
                {
                    b.HasOne("WebDataProject.Data.Models.Teacher", "Leader")
                        .WithMany("AGs")
                        .HasForeignKey("Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Leader");
                });

            modelBuilder.Entity("WebDataProject.Data.Models.Class", b =>
                {
                    b.HasOne("WebDataProject.Data.Models.Teacher", "Teacher")
                        .WithOne("Class")
                        .HasForeignKey("WebDataProject.Data.Models.Class", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Teacher");
                });

            modelBuilder.Entity("WebDataProject.Data.Models.Student", b =>
                {
                    b.HasOne("WebDataProject.Data.Models.Class", "Class")
                        .WithMany("Students")
                        .HasForeignKey("Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Class");
                });

            modelBuilder.Entity("WebDataProject.Data.Models.Class", b =>
                {
                    b.Navigation("Students");
                });

            modelBuilder.Entity("WebDataProject.Data.Models.Teacher", b =>
                {
                    b.Navigation("AGs");

                    b.Navigation("Class");
                });
#pragma warning restore 612, 618
        }
    }
}
