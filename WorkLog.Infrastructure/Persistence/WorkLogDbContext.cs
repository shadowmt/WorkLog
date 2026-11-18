using Microsoft.EntityFrameworkCore;

namespace WorkLog.Infrastructure.Persistence;

public sealed class WorkLogDbContext(DbContextOptions<WorkLogDbContext> options) : DbContext(options);
