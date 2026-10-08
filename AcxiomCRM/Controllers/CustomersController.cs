using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.CustomerName.Contains(search)
                                  || c.Email.Contains(search)
                                  || c.Phone.Contains(search)
                                  || (c.CompanyName != null && c.CompanyName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;

        var list = await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CustomerViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerViewModel model)
    {
        var email = model.Email.Trim().ToLower();

        // Server-side duplicate checks
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == email))
        {
            ModelState.AddModelError("Email", "A customer with this email address already exists.");
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone))
        {
            ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = new Customer
        {
            CustomerCode = Guid.NewGuid().ToString(), // Temporary, updated after save
            CustomerName = model.CustomerName.Trim(),
            Email = email,
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Address = model.Address?.Trim(),
            City = model.City?.Trim(),
            State = model.State?.Trim(),
            Status = model.Status,
            CreatedDate = DateTime.Now,
            CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        customer.CustomerCode = $"CUST-{customer.CustomerId:0000}";
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Customer created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null)
        {
            return NotFound();
        }

        var model = new CustomerViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerViewModel model)
    {
        if (id != model.CustomerId)
        {
            return BadRequest();
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null)
        {
            return NotFound();
        }

        var email = model.Email.Trim().ToLower();

        // Server-side duplicate checks excluding current record
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == email && c.CustomerId != id))
        {
            ModelState.AddModelError("Email", "Another customer is already registered with this email address.");
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone && c.CustomerId != id))
        {
            ModelState.AddModelError("Phone", "Another customer is already registered with this phone number.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        customer.CustomerName = model.CustomerName.Trim();
        customer.Email = email;
        customer.Phone = model.Phone.Trim();
        customer.CompanyName = model.CompanyName?.Trim();
        customer.Address = model.Address?.Trim();
        customer.City = model.City?.Trim();
        customer.State = model.State?.Trim();
        customer.Status = model.Status;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null)
        {
            return NotFound();
        }

        // Deactivate according to existing model
        customer.Status = "Inactive";
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Customer deactivated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
