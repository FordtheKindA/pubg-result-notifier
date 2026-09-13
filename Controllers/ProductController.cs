using Microsoft.AspNetCore.Mvc;
using ServicePractice.Services;
using ServicePractice.DTOs;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;


namespace ServicePractice.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductController(ProductService productService)
    {
        _productService = productService;
    }
[HttpGet("{id}")]
public async Task<IActionResult> GetByIdAsync(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return Ok(product);
    }

[HttpGet]
public async Task<IActionResult> GetAllAsync()
{
    var product = await _productService.GetAllAsync();
    return Ok(product);
}
[HttpPost]
public async Task<IActionResult> CreateAsync(CreateProductRequest request)
    {
        var product = await _productService.CreateAsync(request);

        if (product == null)
        {
            return Conflict("Product already exists.");
        }

return StatusCode(201, product);
    }
[HttpPut("{id}")]
public async Task<IActionResult> UpdateAsync(int id, UpdateProductRequest request)
{
    // เรียก service.Update(...)
    var product = await _productService.UpdateAsync(id,request);

        // ถ้า service คืน null → ?
        if (product == null)
        {
            return NotFound();
        }
    // ถ้าเจอ → ?
    return StatusCode(200, product);
}
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteAsync(int id)
{
    // เรียก _productService.Delete(id)
    var product = await _productService.DeleteAsync(id);

        // ถ้า null → 404
        if (product == null)
        {
            return NotFound();
        }

    // ถ้าสำเร็จ → ?
    return StatusCode(204);
}

}

