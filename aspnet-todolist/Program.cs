using aspnet_todolist.DTOs;
using aspnet_todolist.Exceptions;
using aspnet_todolist.Models;
using aspnet_todolist.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace aspnet_todolist
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    Activity? activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                    context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
                };
            });
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            builder.Services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPropertiesAndHeaders |
                                       Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponsePropertiesAndHeaders;
            });

            builder.Services.AddDbContext<TodoDb>(opt =>
                opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddScoped<ITodoService, TodoService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();

            builder.Services.AddOpenApi();
            builder.Services.AddValidation();

            builder.Services.AddAutoMapper(typeof(Program));

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TodoDb>();
                db.Database.Migrate();

                if (app.Environment.IsDevelopment())
                {
                    await DataSeeder.SeedAsync(db);
                }
            }

            app.UseHttpLogging();
            app.UseExceptionHandler();

            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "v1");
                options.RoutePrefix = "swagger";
            });

            /// <summary>
            /// Retrieves all todo items.
            /// </summary>
            /// <param name="isComplete">Optional filter to get only completed or incomplete todos.</param>
            /// <returns>A list of todo items.</returns>
            /// <response code="200">Returns the list of todo items.</response>
            app.MapGet("/api/todos", async (ITodoService todoService,
                bool? isComplete,
                bool? isDeleted,
                string? search,
                string? sortBy,
                int? page,
                int pageSize = 10,
                bool showDeleted = false,
                string sortOrder = "asc") =>
            {
                var result = await todoService.GetAllAsync(
                    isComplete,
                    isDeleted,
                    search,
                    sortBy,
                    page,
                    pageSize,
                    showDeleted,
                    sortOrder);

                return Results.Ok(result);
            })
            .WithTags("Todos")
            .WithSummary("Retrieves all todo items")
            .WithDescription("Gets a list of all todo items. Optionally filter by completion status using the isComplete query parameter. Search todos by name using the search parameter. Sort using sortBy (id, name, iscomplete, isdeleted) and sortOrder (asc, desc) parameters.");

            /// <summary>
            /// Retrieves a specific todo item by id.
            /// </summary>
            /// <param name="id">The id of the todo item to retrieve.</param>
            /// <returns>The todo item with the specified id.</returns>
            /// <response code="200">Returns the todo item.</response>
            /// <response code="404">If the todo item is not found.</response>
            app.MapGet("/api/todos/{id}", async (int id, ITodoService todoService) =>
            {
                var todo = await todoService.GetByIdAsync(id);
                if (todo == null) return Results.NotFound();

                return Results.Ok(todo);
            })
            .WithTags("Todos")
            .WithSummary("Retrieves a specific todo item")
            .WithDescription("Gets a single todo item by its unique identifier.");

            /// <summary>
            /// Creates a new todo item.
            /// </summary>
            /// <param name="todoDto">The todo item to create.</param>
            /// <returns>The newly created todo item.</returns>
            /// <response code="201">Returns the newly created todo item.</response>
            /// <response code="400">If the provided CategoryId does not exist.</response>
            app.MapPost("/api/todos", async (TodoCreateDto todoDto, ITodoService todoService) =>
            {
                try
                {
                    var createdTodo = await todoService.CreateAsync(todoDto);
                    return Results.Created($"/todolist/{createdTodo.Id}", createdTodo);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
            .WithTags("Todos")
            .WithSummary("Creates a new todo item")
            .WithDescription("Creates a new todo item and adds it to the list.");

            /// <summary>
            /// Updates an existing todo item.
            /// </summary>
            /// <param name="id">The id of the todo item to update.</param>
            /// <param name="todoDto">The updated todo item data.</param>
            /// <returns>The updated todo item.</returns>
            /// <response code="200">Returns the updated todo item.</response>
            /// <response code="404">If the todo item is not found.</response>
            /// <response code="400">If the provided CategoryId does not exist.</response>
            app.MapPut("/api/todos/{id}", async (int id, TodoUpdateDto todoDto, ITodoService todoService) =>
            {
                try
                {
                    var updatedTodo = await todoService.UpdateAsync(id, todoDto);
                    if (updatedTodo == null) return Results.NotFound();

                    return Results.Ok(updatedTodo);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
            .WithTags("Todos")
            .WithSummary("Updates an existing todo item")
            .WithDescription("Updates the name and completion status of an existing todo item.");

            /// <summary>
            /// Deletes a specific todo item.
            /// </summary>
            /// <param name="id">The id of the todo item to delete.</param>
            /// <returns>No content.</returns>
            /// <response code="204">If the todo item was successfully deleted.</response>
            /// <response code="404">If the todo item is not found.</response>
            app.MapDelete("/api/todos/{id}", async (int id, ITodoService todoService, bool hardDelete = false) =>
            {
                var success = await todoService.DeleteAsync(id, hardDelete);
                if (!success) return Results.NotFound();

                return Results.NoContent();
            })
            .WithTags("Todos")
            .WithSummary("Deletes a specific todo item")
            .WithDescription("Removes a todo item from the list by its unique identifier.");

            /// <summary>
            /// Retrieves all categories.
            /// </summary>
            /// <returns>A list of categories.</returns>
            /// <response code="200">Returns the list of categories.</response>
            app.MapGet("/api/categories", async (ICategoryService categoryService) =>
            {
                var categories = await categoryService.GetAllAsync();
                return Results.Ok(categories);
            })
            .WithTags("Categories")
            .WithSummary("Retrieves all categories")
            .WithDescription("Gets a list of all categories.");

            /// <summary>
            /// Retrieves a specific category by id.
            /// </summary>
            /// <param name="id">The id of the category to retrieve.</param>
            /// <returns>The category with the specified id.</returns>
            /// <response code="200">Returns the category.</response>
            /// <response code="404">If the category is not found.</response>
            app.MapGet("/api/categories/{id}", async (int id, ICategoryService categoryService) =>
            {
                var category = await categoryService.GetByIdAsync(id);
                if (category == null) return Results.NotFound();

                return Results.Ok(category);
            })
            .WithTags("Categories")
            .WithSummary("Retrieves a specific category")
            .WithDescription("Gets a single category by its unique identifier.");

            /// <summary>
            /// Creates a new category.
            /// </summary>
            /// <param name="category">The category to create.</param>
            /// <returns>The newly created category.</returns>
            /// <response code="201">Returns the newly created category.</response>
            app.MapPost("/api/categories", async (CategoryCreateDto categoryDto, ICategoryService categoryService) =>
            {
                var createdCategory = await categoryService.CreateAsync(categoryDto);
                return Results.Created($"/api/categories/{createdCategory.Id}", createdCategory);
            })
            .WithTags("Categories")
            .WithSummary("Creates a new category")
            .WithDescription("Creates a new category and adds it to the list.");

            /// <summary>
            /// Updates an existing category.
            /// </summary>
            /// <param name="id">The id of the category to update.</param>
            /// <param name="inputCategory">The updated category data.</param>
            /// <returns>The updated category.</returns>
            /// <response code="200">Returns the updated category.</response>
            /// <response code="404">If the category is not found.</response>
            app.MapPut("/api/categories/{id}", async (int id, CategoryUpdateDto categoryDto, ICategoryService categoryService) =>
            {
                var updatedCategory = await categoryService.UpdateAsync(id, categoryDto);
                if (updatedCategory == null) return Results.NotFound();

                return Results.Ok(updatedCategory);
            })
            .WithTags("Categories")
            .WithSummary("Updates an existing category")
            .WithDescription("Updates the name and color of an existing category.");

            /// <summary>
            /// Deletes a specific category.
            /// </summary>
            /// <param name="id">The id of the category to delete.</param>
            /// <returns>No content.</returns>
            /// <response code="204">If the category was successfully deleted.</response>
            /// <response code="404">If the category is not found.</response>
            app.MapDelete("/api/categories/{id}", async (int id, ICategoryService categoryService) =>
            {
                var success = await categoryService.DeleteAsync(id);
                if (!success) return Results.NotFound();

                return Results.NoContent();
            })
            .WithTags("Categories")
            .WithSummary("Deletes a specific category")
            .WithDescription("Removes a category from the list by its unique identifier.");

            if (app.Environment.IsDevelopment())
            {
                /// <summary>
                /// Resets and reseeds the database with sample data.
                /// </summary>
                /// <returns>A summary of seeded data.</returns>
                /// <response code="200">Returns the count of seeded items.</response>
                app.MapPost("/seed", async (TodoDb db) =>
                {
                    db.Todos.RemoveRange(db.Todos);
                    db.Categories.RemoveRange(db.Categories);
                    await db.SaveChangesAsync();

                    (var categoriesCount, var todosCount) = await DataSeeder.SeedAsync(db);

                    return Results.Ok(new
                    {
                        message = "Database reset and reseeded successfully",
                        categoriesCreated = categoriesCount,
                        todosCreated = todosCount,
                    });
                })
                .WithTags("Development")
                .WithSummary("Resets and reseeds the database")
                .WithDescription("Development only: Clears all existing data and reseeds the database with sample categories and todos.");
            }

            app.Run();


        }
    }
}
