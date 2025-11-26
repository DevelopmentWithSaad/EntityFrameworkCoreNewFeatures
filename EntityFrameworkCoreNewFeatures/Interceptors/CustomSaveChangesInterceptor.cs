using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EntityFrameworkCoreNewFeatures.Interceptors
{
    public class CustomSaveChangesInterceptor : SaveChangesInterceptor
    {
        /// <summary>
        /// Called when SaveChanges fails
        /// Great for error logging and monitoring
        /// </summary>
        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            base.SaveChangesFailed(eventData);
        }
        /// <summary>
        /// Called when SaveChangesAsync fails
        /// </summary>
        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }
        /// <summary>
        /// Called before SaveChanges is executed
        /// Perfect place for logging, validation, or auditing
        /// </summary>
        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            LogChanges(eventData.Context);
            return base.SavedChanges(eventData, result);
        }
        /// <summary>
        /// Called before SaveChangesAsync is executed
        /// </summary>
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            LogChanges(eventData.Context);
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }
        /// <summary>
        /// Called after SaveChanges completes successfully
        /// </summary>
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Console.WriteLine($"[Interceptor] SaveChanges completed. {result} entities affected.");
            return base.SavingChanges(eventData, result);
        }
        /// <summary>
        /// Called after SaveChangesAsync completes successfully
        /// </summary>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
        /// <summary>
        /// Helper method to log all pending changes
        /// This demonstrates how to inspect the ChangeTracker
        /// </summary>

        private void LogChanges(DbContext? dbContext)
        {
            if(dbContext == null)
            {
                return;
            }
            var entries = dbContext.ChangeTracker.Entries()
                    .Where(e=> e.State == EntityState.Added ||
                    e.State == EntityState.Modified ||
                    e.State == EntityState.Deleted);
            foreach (var item in entries)
            {
                var entityname = item.Entity.GetType().Name;
                var state= item.State.ToString();
                Console.WriteLine($"Interceptor {state}: {entityname}");//we can save this to db or file
                if(item.State == EntityState.Added)
                {
                    Console.WriteLine("New Record Added");//we can save this to db or file
                }
                else if(item.State == EntityState.Modified)
                {
                    foreach (var property in item.Properties)
                    {
                        var originalvalue = property.OriginalValue;
                        var newvalue = property.CurrentValue;
                        Console.WriteLine($" {property.Metadata.Name} : {originalvalue} - {newvalue}");//we can save this to db or file
                    }
                }
            }

        }

    }
}
