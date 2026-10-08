using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Dtos;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICrmAuthorizationService _authService;

    public CustomersController(ApplicationDbContext context, ICrmAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    // GET: /api/customers
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _context.Customers
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => MapToDto(c))
            .ToListAsync();

        return Ok(customers);
    }

    // GET: /api/customers/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} was not found." });
        }

        return Ok(MapToDto(customer));
    }

    // POST: /api/customers
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var email = dto.Email.Trim().ToLower();
        var phone = dto.Phone.Trim();

        // Server-side duplicate validation
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == email))
        {
            return BadRequest(new { message = "A customer with this email address already exists." });
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == phone))
        {
            return BadRequest(new { message = "A customer with this phone number already exists." });
        }

        var customer = new Customer
        {
            CustomerCode = Guid.NewGuid().ToString(), // Temporary placeholder
            CustomerName = dto.CustomerName.Trim(),
            Email = email,
            Phone = phone,
            CompanyName = dto.CompanyName?.Trim(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            Status = dto.Status,
            CreatedDate = DateTime.Now,
            CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Assign formatted sequential CustomerCode
        customer.CustomerCode = $"CUST-{customer.CustomerId:0000}";
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, MapToDto(customer));
    }

    // PUT: /api/customers/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} was not found." });
        }

        var email = dto.Email.Trim().ToLower();
        var phone = dto.Phone.Trim();

        // Server-side duplicate validation (excluding current record)
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == email && c.CustomerId != id))
        {
            return BadRequest(new { message = "A customer with this email address already exists." });
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == phone && c.CustomerId != id))
        {
            return BadRequest(new { message = "A customer with this phone number already exists." });
        }

        customer.CustomerName = dto.CustomerName.Trim();
        customer.Email = email;
        customer.Phone = phone;
        customer.CompanyName = dto.CompanyName?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.City = dto.City?.Trim();
        customer.State = dto.State?.Trim();
        customer.Status = dto.Status;

        _context.Update(customer);
        await _context.SaveChangesAsync();

        return Ok(MapToDto(customer));
    }

    // DELETE: /api/customers/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} was not found." });
        }

        // Deactivate according to existing model
        customer.Status = "Inactive";
        _context.Update(customer);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static CustomerDto MapToDto(Customer c)
    {
        return new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            CreatedBy = c.CreatedBy
        };
    }
}
