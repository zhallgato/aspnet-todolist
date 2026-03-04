using aspnet_todolist.DTOs;
using aspnet_todolist.Models;
using AutoMapper;

public class TodoMappingProfile : Profile
{
    public TodoMappingProfile()
    {
        CreateMap<Todo, TodoResponseDto>();
        CreateMap<TodoCreateDto, Todo>();
        CreateMap<TodoUpdateDto, Todo>();
    }
}
