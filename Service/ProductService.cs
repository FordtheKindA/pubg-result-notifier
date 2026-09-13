using ServicePractice.Models;
using ServicePractice.DTOs;
using Microsoft.Extensions.Options;
using ServicePractice.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ServicePractice.Services;

public class ProductService
{
private readonly AppDbContext _db;

public ProductService(AppDbContext db)
{
    _db = db;
}
    
public async Task<Product?> GetByIdAsync(int id)
{
    Product? result = await _db.Products
        .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

    return result;
}

public async Task<List<Product>> GetAllAsync()
{
    
    List<Product> Result = await _db.Products.Where(x=>x.IsActive).OrderBy(x=>x.Name).ToListAsync() ; 
    return Result;
    
}

public async Task<Product?> CreateAsync(CreateProductRequest request)
{
    bool isDuplicate = await _db.Products.AnyAsync(x=>x.Name==request.Name&&x.IsActive);
        if (isDuplicate)
        {
            return null;
        }
        var product = new Product {Name=request.Name,Price=request.Price,IsActive=true};
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
}
public async Task<Product?> UpdateAsync(int id, UpdateProductRequest request)
{
    // หา id และต้อง IsActive
    var idFind = await _db.Products.FirstOrDefaultAsync(x=>x.Id==id&&x.IsActive);
        // ไม่เจอ return null
        if (idFind == null)
        {
            return null;
        }
    // เจอแล้วแก้ Name
    idFind.Name = request.Name;
    idFind.Price = request.Price;
    await _db.SaveChangesAsync();
    // return product
    return idFind ;
}
public async Task<Product?> DeleteAsync(int id)
{
    var product = await _db.Products.FirstOrDefaultAsync(x=>x.Id==id&&x.IsActive);
        if (product == null)
        {
            return null;
        }
        
    product.IsActive = false;
    await _db.SaveChangesAsync();
    return product;
}
}
