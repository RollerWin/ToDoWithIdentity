using System.ComponentModel.DataAnnotations;

namespace IdentityTrain2.Models.DTO
{
    public class ToDoListCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }
    }
}
