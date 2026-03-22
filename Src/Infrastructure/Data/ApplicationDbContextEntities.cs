using Domain.Aggregates.Identity;
using Domain.Aggregates.Storage;
using Domain.Common.Interfaces;
using Domain.Aggregates.Licensing;
using Infrastructure.Messaging.Dto;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<UserProfile> Profiles { get; set; } = null!;
    public DbSet<FileMetadata> FileMetadata { get; set; } = null!;
    public DbSet<Domain.Aggregates.Licensing.License> Licenses { get; set; } = null!;
}
