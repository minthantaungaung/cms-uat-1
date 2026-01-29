//using Microsoft.EntityFrameworkCore;

//namespace cms_api
//{
//    using Bogus;
//    using Microsoft.EntityFrameworkCore;
//    using Microsoft.EntityFrameworkCore.Metadata;
//    using Microsoft.Extensions.Hosting;
//    using System.Collections;
//    using System.Reflection;

//    public static class DevDummyDataSeeder
//    {
//        public static async Task SeedAsync(
//            DbContext context,
//            IHostEnvironment env,
//            int rowsPerEntity = 500,
//            bool useBulkInsert = false)
//        {
//            if (!env.IsDevelopment())
//                return;

//            context.ChangeTracker.Clear();

//            var model = context.Model;
//            var faker = new Faker();

//            // Insert principals first, dependents later
//            var entityTypes = model.GetEntityTypes()
//                .Where(e => !e.IsOwned())
//                .OrderBy(e => e.GetForeignKeys().Count())
//                .ToList();

//            foreach (var entityType in entityTypes)
//            {
//                var queryable = context.SetDynamic(entityType.ClrType);

//                if (await queryable.AnyAsync())
//                    continue;

//                var entities = new List<object>();

//                for (int i = 0; i < rowsPerEntity; i++)
//                {
//                    var entity = Activator.CreateInstance(entityType.ClrType)!;

//                    foreach (var prop in entityType.GetProperties())
//                    {
//                        if (prop.IsPrimaryKey())
//                            continue;

//                        if (prop.IsForeignKey())
//                            continue;

//                        var value = GenerateValue(prop, faker);
//                        prop.PropertyInfo?.SetValue(entity, value);
//                    }

//                    entities.Add(entity);
//                }

//                if (useBulkInsert)
//                {
//                    // Requires EFCore.BulkExtensions
//                    await context.BulkInsertAsync(entities);
//                }
//                else
//                {
//                    context.AddRange(entities);
//                    await context.SaveChangesAsync();
//                }
//            }

//            await ResolveForeignKeysAsync(context, model);
//        }

//        // -----------------------------
//        // VALUE GENERATION
//        // -----------------------------

//        private static object? GenerateValue(IProperty prop, Faker faker)
//        {
//            var clrType = prop.ClrType;
//            var isNullable = Nullable.GetUnderlyingType(clrType) != null;
//            clrType = Nullable.GetUnderlyingType(clrType) ?? clrType;

//            if (isNullable && faker.Random.Bool(0.2f))
//                return null;

//            if (clrType == typeof(string))
//            {
//                var maxLength = prop.GetMaxLength() ?? 64;
//                var value = faker.Lorem.Word();
//                return value[..Math.Min(value.Length, maxLength)];
//            }

//            if (clrType == typeof(int))
//                return faker.Random.Int(1, 100_000);

//            if (clrType == typeof(long))
//                return faker.Random.Long(1, 1_000_000);

//            if (clrType == typeof(bool))
//                return faker.Random.Bool();

//            if (clrType == typeof(DateTime))
//                return faker.Date.Past(5);

//            if (clrType == typeof(decimal))
//            {
//                var precision = prop.GetPrecision() ?? 18;
//                var scale = prop.GetScale() ?? 2;

//                var max = (decimal)Math.Pow(10, precision - scale) - 1;
//                var value = faker.Random.Decimal(1, max);

//                return Math.Round(value, scale);
//            }

//            if (clrType.IsEnum)
//                return faker.PickRandom(Enum.GetValues(clrType));

//            return null;
//        }

//        // -----------------------------
//        // FOREIGN KEY RESOLUTION
//        // -----------------------------

//        private static async Task ResolveForeignKeysAsync(
//            DbContext context,
//            IModel model)
//        {
//            foreach (var entityType in model.GetEntityTypes())
//            {
//                foreach (var fk in entityType.GetForeignKeys())
//                {
//                    var principalQuery = context.SetDynamic(
//                        fk.PrincipalEntityType.ClrType);

//                    var principals = await principalQuery
//                        .AsNoTracking()
//                        .ToListAsync();

//                    if (!principals.Any())
//                        continue;

//                    var dependentQuery = context.SetDynamic(entityType.ClrType);
//                    var dependents = await dependentQuery.ToListAsync();

//                    var pkProp = fk.PrincipalKey.Properties.Single();

//                    foreach (var dependent in dependents)
//                    {
//                        var principal =
//                            principals[Random.Shared.Next(principals.Count)];

//                        var pkValue =
//                            pkProp.PropertyInfo!.GetValue(principal);

//                        foreach (var fkProp in fk.Properties)
//                        {
//                            fkProp.PropertyInfo!.SetValue(dependent, pkValue);
//                        }
//                    }
//                }
//            }

//            await context.SaveChangesAsync();
//        }
//    }

//}
//public static class DbContextExtensions
//{
//    public static IQueryable SetDynamic(this DbContext context, Type entityType)
//    {
//        var method = typeof(DbContext)
//            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
//            .MakeGenericMethod(entityType);

//        return (IQueryable)method.Invoke(context, null)!;
//    }
//}
//public static class QueryableExtensions
//{
//    public static IQueryable AsNoTrackingDynamic(this IQueryable source)
//    {
//        var entityType = source.ElementType;

//        var method = typeof(EntityFrameworkQueryableExtensions)
//            .GetMethods()
//            .Single(m =>
//                m.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking) &&
//                m.IsGenericMethod &&
//                m.GetParameters().Length == 1)
//            .MakeGenericMethod(entityType);

//        return (IQueryable)method.Invoke(null, new object[] { source })!;
//    }

//    public static async Task<List<object>> ToListDynamicAsync(this IQueryable source)
//    {
//        var entityType = source.ElementType;

//        var method = typeof(EntityFrameworkQueryableExtensions)
//            .GetMethods()
//            .Single(m =>
//                m.Name == nameof(EntityFrameworkQueryableExtensions.ToListAsync) &&
//                m.IsGenericMethod)
//            .MakeGenericMethod(entityType);

//        var task = (Task)method.Invoke(null, new object[] { source, CancellationToken.None })!;
//        await task;

//        return (List<object>)task
//            .GetType()
//            .GetProperty("Result")!
//            .GetValue(task)!;
//    }

//    public static async Task<bool> AnyDynamicAsync(this IQueryable source)
//    {
//        var entityType = source.ElementType;

//        var method = typeof(EntityFrameworkQueryableExtensions)
//            .GetMethods()
//            .Single(m =>
//                m.Name == nameof(EntityFrameworkQueryableExtensions.AnyAsync) &&
//                m.IsGenericMethod)
//            .MakeGenericMethod(entityType);

//        var task = (Task)method.Invoke(null, new object[] { source, CancellationToken.None })!;
//        await task;

//        return (bool)task
//            .GetType()
//            .GetProperty("Result")!
//            .GetValue(task)!;
//    }
//}
