using System.ComponentModel.DataAnnotations;

namespace MovieBox.Models;

public class CategorizedItems
{
    [Key]
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int MovieId { get; set; }
    public virtual Movie Movie { get; set; }
    public virtual Category Category { get; set; }

}