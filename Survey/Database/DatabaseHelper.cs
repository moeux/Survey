using System.Collections.Immutable;
using AutoCommand.Utils;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;
using Survey.Database.Models;

namespace Survey.Database;

public static class DatabaseHelper
{
    private static readonly Logger Logger = new LoggerConfiguration()
        .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
        .WriteTo.File(
            EnvironmentUtils.GetVariable("SURVEY_LOG_FILE", "survey-.log"),
            rollingInterval: RollingInterval.Day)
        .CreateLogger();

    private static DatabaseContext CreateDatabaseContext()
    {
        var dbContext = new DatabaseContext();

        dbContext.SaveChangesFailed += (_, args) =>
        {
            Logger.Error(args.Exception, "An error occurred while saving database changes");
        };
        dbContext.SavedChanges += (_, args) =>
        {
            Logger.Information("Saved {EntitiesSavedCount} entities to the database", args.EntitiesSavedCount);
        };

        return dbContext;
    }

    public static bool AddSuggestion(Suggestion suggestion)
    {
        using var dbContext = CreateDatabaseContext();
        dbContext.Suggestions.Add(suggestion);

        return dbContext.SaveChanges() > 0;
    }

    public static IEnumerable<Suggestion> GetSuggestions()
    {
        using var dbContext = CreateDatabaseContext();

        return dbContext.Suggestions.ToImmutableArray();
    }
}