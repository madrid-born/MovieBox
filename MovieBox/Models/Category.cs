using System.ComponentModel.DataAnnotations;

namespace MovieBox.Models;

public class Category
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public int ListId { get; set; }
    public virtual ICollection<CategorizedItems>? CategorizedItems { get; set; }
    public virtual List List { get; set; }
}