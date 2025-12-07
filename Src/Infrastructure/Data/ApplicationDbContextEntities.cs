using Infrastructure.Messaging.Dto;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
}
