using BlazorApp3.Wasm.Sqlite.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Data.Common;
using System.Runtime.InteropServices;

namespace BlazorApp3.Wasm.Sqlite.Data
{
    public class DataSynchronizer
    {
        public const string SqliteDbFilename = "app.db";
        private readonly Task firstTimeSetupTask;
        private readonly IDbContextFactory<ClientSideDbContext> dbContextFactory;
        private bool isSynchronizing;

        public DataSynchronizer(IJSRuntime js, IDbContextFactory<ClientSideDbContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory;
            firstTimeSetupTask = FirstTimeSetupAsync(js);
        }

        public int SyncCompleted { get; private set; }
        public int SyncTotal { get; private set; }

        public async Task<ClientSideDbContext> GetPreparedDbContextAsync()
        {
            await firstTimeSetupTask;
            return await dbContextFactory.CreateDbContextAsync();
        }

        public void SynchronizeInBackground()
        {
            _ = EnsureSynchronizingAsync();
        }

        public event Action? OnUpdate;
        public event Action<Exception>? OnError;

        private async Task FirstTimeSetupAsync(IJSRuntime js)
        {
            //var module = await js.InvokeAsync<IJSObjectReference>("import", "./dbstorage.js");

            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser")))
            //{
            //    await module.InvokeVoidAsync("synchronizeFileWithIndexedDb", SqliteDbFilename);
            //}

            using var db = await dbContextFactory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();
        }

        private async Task EnsureSynchronizingAsync()
        {
            // Don't run multiple syncs in parallel. This simple logic is adequate because of single-threadedness.
            if (isSynchronizing)
            {
                return;
            }

            try
            {
                isSynchronizing = true;


                List<Person> people = new List<Person>()
                {
                    new Person
                    {
                        Id = 1,
                        Name = "Lautaro Carro",
                        Age = 24,
                        Birthday = new DateTime(1998, 07, 02),
                        IsActive = true,
                    }
                };

                SyncTotal = people.Count;
                SyncCompleted = 0;

                // Get a DB context
                using var db = await GetPreparedDbContextAsync();
                db.ChangeTracker.AutoDetectChangesEnabled = false;
                db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;


                var connection = db.Database.GetDbConnection();
                connection.Open();

               

                BulkInsert(connection, people);

                SyncCompleted = people.Count;

                OnUpdate?.Invoke();
            }
            catch (Exception ex)
            {
                // TODO: use logger also
                OnError?.Invoke(ex);
            }
            finally
            {
                isSynchronizing = false;
            }
        }

        private void BulkInsert(DbConnection connection, IEnumerable<Person> parts)
        {
            // Since we're inserting so much data, we can save a huge amount of time by dropping down below EF Core and
            // using the fastest bulk insertion technique for Sqlite.
            using (var transaction = connection.BeginTransaction())
            {
                var command = connection.CreateCommand();
                var id = AddNamedParameter(command, "$Id");
                var name = AddNamedParameter(command, "$Name");
                var age = AddNamedParameter(command, "$Age");
                var birthday = AddNamedParameter(command, "$Birthday");
                var isactive = AddNamedParameter(command, "$IsActive");

                command.CommandText =
                    $"INSERT OR REPLACE INTO Persons (Id, Name, Age, Birthday, IsActive) " +
                    $"VALUES ({id.ParameterName}, {name.ParameterName}, {age.ParameterName}, {birthday.ParameterName}, {isactive.ParameterName})";

                foreach (var part in parts)
                {
                    id.Value = part.Id;
                    name.Value = part.Name;
                    age.Value = part.Age;
                    birthday.Value = part.Birthday;
                    isactive.Value = part.IsActive;
            
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            static DbParameter AddNamedParameter(DbCommand command, string name)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                command.Parameters.Add(parameter);
                return parameter;
            }
        }
    }
}
