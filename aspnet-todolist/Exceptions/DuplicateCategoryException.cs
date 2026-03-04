namespace aspnet_todolist.Exceptions
{
    public class DuplicateCategoryException(string name) : Exception($"A category with the name '{name}' already exists.")
    {
    }
}
