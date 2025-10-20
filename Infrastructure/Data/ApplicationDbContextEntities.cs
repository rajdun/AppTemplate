using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
}