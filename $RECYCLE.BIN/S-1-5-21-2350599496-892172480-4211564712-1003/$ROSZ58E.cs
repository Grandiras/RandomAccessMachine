using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebDataProject.Data.Models;
public class Class
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public string Name { get; set; } = "";

    [ForeignKey(nameof(Teacher.Id))]
    public Teacher? Teacher { get; set; }
    public ICollection<Student>? Students { get; set; }
}
