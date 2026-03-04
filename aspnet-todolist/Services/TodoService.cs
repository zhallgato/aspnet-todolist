using aspnet_todolist.DTOs;
using aspnet_todolist.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace aspnet_todolist.Services
{
    public class TodoService : ITodoService
    {
        private readonly TodoDb _db;
        private readonly IMapper _mapper;

        public TodoService(TodoDb db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<object> GetAllAsync(
            bool? isComplete = null,
            bool? isDeleted = null,
            string? search = null,
            string? sortBy = null,
            int? page = null,
            int pageSize = 10,
            bool showDeleted = false,
            string sortOrder = "asc")
        {
            var query = _db.Todos.Include(t => t.Category).AsQueryable();

            if (!showDeleted && !isDeleted.HasValue)
                query = query.Where(t => t.IsDeleted == false);

            if (isDeleted.HasValue)
                query = query.Where(t => t.IsDeleted == isDeleted.Value);

            if (isComplete.HasValue)
                query = query.Where(t => t.IsComplete == isComplete.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.Name!.Contains(search));

            if (!string.IsNullOrEmpty(sortBy))
            {
                var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

                query = sortBy.ToLower() switch
                {
                    "id" => isDescending ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id),
                    "name" => isDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                    "complete" => isDescending ? query.OrderByDescending(t => t.IsComplete) : query.OrderBy(t => t.IsComplete),
                    _ => query
                };
            }

            if (page.HasValue)
            {
                var totalCount = await query.CountAsync();
                var items = await query.Skip(((int)page - 1) * pageSize).Take(pageSize).ToListAsync();

                var result = new PagedResult<TodoResponseDto>
                {
                    Items = _mapper.Map<List<TodoResponseDto>>(items),
                    TotalCount = totalCount,
                    CurrentPage = (int)page,
                    PageSize = pageSize
                };

                return result;
            }

            var todos = await query.ToListAsync();
            return _mapper.Map<List<TodoResponseDto>>(todos);
        }

        public async Task<TodoResponseDto?> GetByIdAsync(int id)
        {
            var todo = await _db.Todos.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id);
            if (todo == null) return null;
            if (todo.IsDeleted == true) return null;

            return _mapper.Map<TodoResponseDto>(todo);
        }

        public async Task<TodoResponseDto> CreateAsync(TodoCreateDto todoDto)
        {
            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == todoDto.CategoryId);
            if (!categoryExists)
                throw new ArgumentException("Category does not exist");

            var todo = new Todo
            {
                Name = todoDto.Name,
                CategoryId = todoDto.CategoryId,
                IsComplete = false
            };

            _db.Todos.Add(todo);
            await _db.SaveChangesAsync();

            await _db.Entry(todo).Reference(t => t.Category).LoadAsync();

            return _mapper.Map<TodoResponseDto>(todo);
        }

        public async Task<TodoResponseDto?> UpdateAsync(int id, TodoUpdateDto todoDto)
        {
            var todo = await _db.Todos.FindAsync(id);

            if (todo == null) return null;
            if (todo.IsDeleted == true) return null;

            if (todoDto.CategoryId.HasValue)
            {
                var categoryExists = await _db.Categories.AnyAsync(c => c.Id == todoDto.CategoryId.Value);
                if (!categoryExists)
                    throw new ArgumentException("Category does not exist");
            }

            todo.Name = todoDto.Name;
            todo.IsComplete = todoDto.IsComplete;
            todo.CategoryId = todoDto.CategoryId;
            await _db.SaveChangesAsync();

            if (todo.CategoryId.HasValue)
            {
                await _db.Entry(todo).Reference(t => t.Category).LoadAsync();
            }

            return _mapper.Map<TodoResponseDto>(todo);
        }

        public async Task<bool> DeleteAsync(int id, bool hardDelete = false)
        {
            var todo = await _db.Todos.FindAsync(id);

            if (todo == null) return false;
            if (todo.IsDeleted == true && !hardDelete) return false;

            if (hardDelete)
                _db.Todos.Remove(todo);
            else
                todo.IsDeleted = true;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
