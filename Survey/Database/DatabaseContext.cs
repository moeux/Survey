using AutoCommand.Utils;
using Microsoft.EntityFrameworkCore;
using Survey.Database.Models;

namespace Survey.Database;

public class DatabaseContext : DbContext
{
    public DbSet<Suggestion> Suggestions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(EnvironmentUtils.GetVariable("SURVEY_DB_CONNECTION_STRING"));
    }
}