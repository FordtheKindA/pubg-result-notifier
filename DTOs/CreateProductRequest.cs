using Microsoft.AspNetCore.Routing.Constraints;
using System.ComponentModel.DataAnnotations;

namespace ServicePractice.DTOs;

public class CreateProductRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(0.01,Double.MaxValue)]    
    public decimal Price { get; set; }
}
