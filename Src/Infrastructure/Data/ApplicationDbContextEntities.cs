using Domain.Aggregates.Identity;
using Infrastructure.Messaging.Dto;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<UserProfile> Profiles { get; set; } = null!;
}
