using System.ComponentModel.DataAnnotations;

namespace MovieBox.Models;

public class List
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public virtual ICollection<Movie>? Movies { get; set; }
    public virtual ICollection<Category>? Categories { get; set; }
}