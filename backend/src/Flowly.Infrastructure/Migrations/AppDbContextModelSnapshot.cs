
using System;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Flowly.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Flowly.Domain.Entities.ArchiveEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("ArchivedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("EntityId")
                        .HasColumnType("uuid");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<string>("PayloadJson")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ArchivedAt");

                    b.HasIndex("EntityType");

                    b.HasIndex("UserId");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("ArchiveEntries", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Budget", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("ArchivedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("CategoryId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CurrencyCode")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<bool>("IsArchived")
                        .HasColumnType("boolean");

                    b.Property<decimal>("Limit")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("PeriodEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("PeriodStart")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.HasIndex("CurrencyCode");

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "PeriodStart", "PeriodEnd");

                    b.ToTable("Budgets", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Category", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Color")
                        .HasColumnType("text");

                    b.Property<string>("Icon")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "Name")
                        .IsUnique();

                    b.ToTable("Categories", (string)null);

                    b.HasData(
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000001"),
                            Name = "Food & Drinks"
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000002"),
                            Name = "Transport"
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000003"),
                            Name = "Shopping"
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000004"),
                            Name = "Entertainment"
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000005"),
                            Name = "Health"
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000006"),
                            Name = "Education"
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000007"),
                            Name = "Utilities"
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000008"),
                            Name = "Salary"
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000009"),
                            Name = "Freelance"
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-00000000000a"),
                            Name = "Other"
                        });
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Currency", b =>
                {
                    b.Property<string>("Code")
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)");

                    b.HasKey("Code");

                    b.ToTable("Currencies");

                    b.HasData(
                        new
                        {
                            Code = "USD",
                            Name = "US Dollar",
                            Symbol = "$"
                        },
                        new
                        {
                            Code = "EUR",
                            Name = "Euro",
                            Symbol = "€"
                        },
                        new
                        {
                            Code = "UAH",
                            Name = "Ukrainian Hryvnia",
                            Symbol = "₴"
                        },
                        new
                        {
                            Code = "PLN",
                            Name = "Polish Zloty",
                            Symbol = "zł"
                        });
                });

            modelBuilder.Entity("Flowly.Domain.Entities.FinancialGoal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CurrencyCode")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)");

                    b.Property<decimal>("CurrentAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<DateTime?>("Deadline")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<bool>("IsArchived")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<decimal>("TargetAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("CurrencyCode");

                    b.HasIndex("Deadline");

                    b.HasIndex("IsArchived");

                    b.HasIndex("UserId");

                    b.ToTable("FinancialGoals", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Link", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("FromId")
                        .HasColumnType("uuid");

                    b.Property<string>("FromType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<Guid?>("NoteId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("NoteId1")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("TaskItemId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("TaskItemId1")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ToId")
                        .HasColumnType("uuid");

                    b.Property<string>("ToType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<Guid?>("TransactionId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("TransactionId1")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("NoteId");

                    b.HasIndex("NoteId1");

                    b.HasIndex("TaskItemId");

                    b.HasIndex("TaskItemId1");

                    b.HasIndex("TransactionId");

                    b.HasIndex("TransactionId1");

                    b.HasIndex("FromType", "FromId");

                    b.HasIndex("ToType", "ToId");

                    b.HasIndex("FromType", "FromId", "ToType", "ToId")
                        .IsUnique();

                    b.ToTable("Links", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.MediaAsset", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("MimeType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<Guid?>("NoteId")
                        .HasColumnType("uuid");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<long>("Size")
                        .HasColumnType("bigint");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("NoteId");

                    b.HasIndex("UserId");

                    b.ToTable("MediaAssets", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Note", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("HtmlCache")
                        .HasColumnType("text");

                    b.Property<bool>("IsArchived")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<string>("Markdown")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("NoteGroupId")
                        .HasColumnType("uuid");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("IsArchived");

                    b.HasIndex("NoteGroupId");

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "IsArchived");

                    b.ToTable("Notes", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.NoteGroup", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Color")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Order")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0);

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "Order");

                    b.ToTable("NoteGroups", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.NoteTag", b =>
                {
                    b.Property<Guid>("NoteId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("TagId")
                        .HasColumnType("uuid");

                    b.HasKey("NoteId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("NoteTags", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.RefreshToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CreatedByIp")
                        .HasMaxLength(45)
                        .HasColumnType("character varying(45)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsRevoked")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<DateTime?>("RevokedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("RevokedByIp")
                        .HasColumnType("text");

                    b.Property<string>("Token")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ExpiresAt");

                    b.HasIndex("Token")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "IsRevoked", "ExpiresAt");

                    b.ToTable("RefreshTokens", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Tag", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Color")
                        .HasMaxLength(7)
                        .HasColumnType("character varying(7)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "Name")
                        .IsUnique();

                    b.ToTable("Tags", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Color")
                        .HasMaxLength(7)
                        .HasColumnType("character varying(7)");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<DateTime?>("DueDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsArchived")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<int>("Order")
                        .HasColumnType("integer");

                    b.Property<string>("Priority")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<Guid?>("TaskThemeId")
                        .HasColumnType("uuid");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("DueDate");

                    b.HasIndex("IsArchived");

                    b.HasIndex("Status");

                    b.HasIndex("TaskThemeId");

                    b.HasIndex("UserId");

                    b.HasIndex("TaskThemeId", "Order");

                    b.ToTable("Tasks", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskRecurrence", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("LastOccurrence")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("NextOccurrence")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Rule")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("TaskItemId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("TaskItemId")
                        .IsUnique();

                    b.ToTable("TaskRecurrences", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskSubtask", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsDone")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<int>("Order")
                        .HasColumnType("integer");

                    b.Property<Guid>("TaskItemId")
                        .HasColumnType("uuid");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.HasKey("Id");

                    b.HasIndex("TaskItemId");

                    b.HasIndex("TaskItemId", "Order");

                    b.ToTable("TaskSubtasks", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskTag", b =>
                {
                    b.Property<Guid>("TaskId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("TagId")
                        .HasColumnType("uuid");

                    b.HasKey("TaskId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("TaskTags", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskTheme", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Color")
                        .HasMaxLength(7)
                        .HasColumnType("character varying(7)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Order")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "Order");

                    b.ToTable("TaskThemes", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Transaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid?>("BudgetId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("CategoryId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CurrencyCode")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<Guid?>("GoalId")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsArchived")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("BudgetId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("CurrencyCode");

                    b.HasIndex("Date");

                    b.HasIndex("GoalId");

                    b.HasIndex("IsArchived");

                    b.HasIndex("Type");

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "Date", "IsArchived");

                    b.ToTable("Transactions", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TransactionTag", b =>
                {
                    b.Property<Guid>("TransactionId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("TagId")
                        .HasColumnType("uuid");

                    b.HasKey("TransactionId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("TransactionTags");
                });

            modelBuilder.Entity("Flowly.Infrastructure.Identity.ApplicationUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<string>("AvatarPath")
                        .HasColumnType("text");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<int>("PreferredTheme")
                        .HasColumnType("integer");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("Users", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("Roles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("RoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("UserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("UserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("UserTokens", (string)null);
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Budget", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.Category", "Category")
                        .WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Flowly.Domain.Entities.Currency", "Currency")
                        .WithMany()
                        .HasForeignKey("CurrencyCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Category");

                    b.Navigation("Currency");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.FinancialGoal", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.Currency", "Currency")
                        .WithMany()
                        .HasForeignKey("CurrencyCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Currency");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Link", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.Note", null)
                        .WithMany("LinksFrom")
                        .HasForeignKey("NoteId");

                    b.HasOne("Flowly.Domain.Entities.Note", null)
                        .WithMany("LinksTo")
                        .HasForeignKey("NoteId1");

                    b.HasOne("Flowly.Domain.Entities.TaskItem", null)
                        .WithMany("LinksFrom")
                        .HasForeignKey("TaskItemId");

                    b.HasOne("Flowly.Domain.Entities.TaskItem", null)
                        .WithMany("LinksTo")
                        .HasForeignKey("TaskItemId1");

                    b.HasOne("Flowly.Domain.Entities.Transaction", null)
                        .WithMany("LinksFrom")
                        .HasForeignKey("TransactionId");

                    b.HasOne("Flowly.Domain.Entities.Transaction", null)
                        .WithMany("LinksTo")
                        .HasForeignKey("TransactionId1");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.MediaAsset", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.Note", "Note")
                        .WithMany("MediaAssets")
                        .HasForeignKey("NoteId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Note");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Note", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.NoteGroup", "NoteGroup")
                        .WithMany("Notes")
                        .HasForeignKey("NoteGroupId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("NoteGroup");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.NoteTag", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.Note", "Note")
                        .WithMany("NoteTags")
                        .HasForeignKey("NoteId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Flowly.Domain.Entities.Tag", "Tag")
                        .WithMany("NoteTags")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Note");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskItem", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.TaskTheme", "TaskTheme")
                        .WithMany("Tasks")
                        .HasForeignKey("TaskThemeId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("TaskTheme");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskRecurrence", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.TaskItem", "TaskItem")
                        .WithOne("Recurrence")
                        .HasForeignKey("Flowly.Domain.Entities.TaskRecurrence", "TaskItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TaskItem");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskSubtask", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.TaskItem", "TaskItem")
                        .WithMany("Subtasks")
                        .HasForeignKey("TaskItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TaskItem");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskTag", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.Tag", "Tag")
                        .WithMany("TaskTags")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Flowly.Domain.Entities.TaskItem", "Task")
                        .WithMany("TaskTags")
                        .HasForeignKey("TaskId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Tag");

                    b.Navigation("Task");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Transaction", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.Budget", "Budget")
                        .WithMany()
                        .HasForeignKey("BudgetId");

                    b.HasOne("Flowly.Domain.Entities.Category", "Category")
                        .WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Flowly.Domain.Entities.Currency", "Currency")
                        .WithMany()
                        .HasForeignKey("CurrencyCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Flowly.Domain.Entities.FinancialGoal", "Goal")
                        .WithMany()
                        .HasForeignKey("GoalId");

                    b.Navigation("Budget");

                    b.Navigation("Category");

                    b.Navigation("Currency");

                    b.Navigation("Goal");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TransactionTag", b =>
                {
                    b.HasOne("Flowly.Domain.Entities.Tag", "Tag")
                        .WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Flowly.Domain.Entities.Transaction", "Transaction")
                        .WithMany("TransactionTags")
                        .HasForeignKey("TransactionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Tag");

                    b.Navigation("Transaction");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.HasOne("Flowly.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.HasOne("Flowly.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Flowly.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.HasOne("Flowly.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Note", b =>
                {
                    b.Navigation("LinksFrom");

                    b.Navigation("LinksTo");

                    b.Navigation("MediaAssets");

                    b.Navigation("NoteTags");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.NoteGroup", b =>
                {
                    b.Navigation("Notes");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Tag", b =>
                {
                    b.Navigation("NoteTags");

                    b.Navigation("TaskTags");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskItem", b =>
                {
                    b.Navigation("LinksFrom");

                    b.Navigation("LinksTo");

                    b.Navigation("Recurrence");

                    b.Navigation("Subtasks");

                    b.Navigation("TaskTags");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.TaskTheme", b =>
                {
                    b.Navigation("Tasks");
                });

            modelBuilder.Entity("Flowly.Domain.Entities.Transaction", b =>
                {
                    b.Navigation("LinksFrom");

                    b.Navigation("LinksTo");

                    b.Navigation("TransactionTags");
                });
#pragma warning restore 612, 618
        }
    }
}
