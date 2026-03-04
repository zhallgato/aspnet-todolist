using System.ComponentModel.DataAnnotations;

namespace aspnet_todolist.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(50)]
        public string? Name { get; set; }

        [Required]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Color must be a valid HEX code")]
        public string? Color { get; set; }

        public ICollection<Todo>? Todos { get; set; }
    }
}
