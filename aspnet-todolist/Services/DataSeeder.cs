using aspnet_todolist.Models;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace aspnet_todolist.Services
{
    public static class DataSeeder
    {
        public async static Task<(int categoriesCount, int todosCount)> SeedAsync(TodoDb db)
        {
            if (await db.Todos.AnyAsync())
                return (0, 0);

            Randomizer.Seed = new Random();
            Random random = new();

            var categoryFaker = new Faker<Category>()
                .RuleFor(u => u.Name, f => f.Lorem.Word())
                .RuleFor(u => u.Color, f => String.Format("#{0:X6}", random.Next(0x1000000)));

            var categories = categoryFaker.Generate(20);
            await db.Categories.AddRangeAsync(categories);
            await db.SaveChangesAsync();

            var todoFaker = new Faker<Todo>()
                .RuleFor(t => t.Name, f => f.Lorem.Word())
                .RuleFor(t => t.IsComplete, f => f.Random.Bool())
                .RuleFor(u => u.CategoryId, f =>
                {
                    var categoryIds = db.Categories.Select(c => c.Id).ToList();
                    return categoryIds.Count > 0 ? f.PickRandom(categoryIds) : (int?)null;
                });

            var todos = todoFaker.Generate(500);
            await db.Todos.AddRangeAsync(todos);
            await db.SaveChangesAsync();

            return (categories.Count, todos.Count);
        }
    }
}
