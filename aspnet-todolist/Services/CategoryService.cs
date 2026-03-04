using aspnet_todolist.DTOs;
using aspnet_todolist.Exceptions;
using aspnet_todolist.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace aspnet_todolist.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly TodoDb _db;
        private readonly IMapper _mapper;

        public CategoryService(TodoDb db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<CategoryResponseDto>> GetAllAsync()
        {
            var categories = await _db.Categories.Include(c => c.Todos).ToListAsync();
            return _mapper.Map<List<CategoryResponseDto>>(categories);
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return null;

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto categoryDto)
        {
            var existingCategory = await _db.Categories
                .AnyAsync(c => c.Name == categoryDto.Name);

            if (existingCategory)
            {
                throw new DuplicateCategoryException(categoryDto.Name);
            }

            var category = new Category
            {
                Name = categoryDto.Name,
                Color = categoryDto.Color
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task<CategoryResponseDto?> UpdateAsync(int id, CategoryUpdateDto categoryDto)
        {
            var category = await _db.Categories.FindAsync(id);

            if (category == null) return null;

            category.Name = categoryDto.Name;
            category.Color = categoryDto.Color;
            await _db.SaveChangesAsync();

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _db.Categories.FindAsync(id);

            if (category == null) return false;

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
