using Domain.Aggregates.Identity;
using Domain.Aggregates.Licencing;
using Domain.Aggregates.Storage;
using Domain.Common.Interfaces;
using Infrastructure.Messaging.Dto;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<UserProfile> Profiles { get; set; } = null!;
    public DbSet<FileMetadata> FileMetadata { get; set; } = null!;
    public DbSet<Domain.Aggregates.Licencing.Licence> Licences { get; set; } = null!;
}
