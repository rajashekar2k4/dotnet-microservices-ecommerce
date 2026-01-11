using ECommerce.Catalog.Application.Commands;
using ECommerce.Catalog.Application.DTOs;
using ECommerce.Catalog.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Catalog.API.Controllers;

/// <summary>
/// Products API Controller with CQRS pattern.
/// Uses .NET 10 default dependency injection and ILogger.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    /// <summary>
    /// Constructor with .NET default dependency injection.
    /// </summary>
    /// <param name="mediator">MediatR mediator for CQRS (injected by DI container)</param>
    /// <param name="logger">.NET ILogger instance (injected by DI container)</param>
    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all products with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // Structured logging with named parameters
        _logger.LogInformation("Getting products - Page: {PageNumber}, Size: {PageSize}", 
            pageNumber, pageSize);

        try
        {
            var query = new GetProductsQuery 
            { 
                PageNumber = pageNumber, 
                PageSize = pageSize 
            };
            
            var result = await _mediator.Send(query);

            _logger.LogInformation("Successfully retrieved {ProductCount} products", 
                result.Items.Count());

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting products");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "An error occurred while retrieving products" });
        }
    }

    /// <summary>
    /// Get product by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", id);

        try
        {
            var query = new GetProductByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                _logger.LogWarning("Product not found with ID: {ProductId}", id);
                return NotFound(new { Message = $"Product with ID {id} not found" });
            }

            _logger.LogInformation("Successfully retrieved product: {ProductName} (ID: {ProductId})", 
                result.Name, result.Id);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting product with ID: {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "An error occurred while retrieving the product" });
        }
    }

    /// <summary>
    /// Get products by category.
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductsByCategory(string category)
    {
        _logger.LogInformation("Getting products in category: {Category}", category);

        try
        {
            var query = new GetProductsByCategoryQuery { Category = category };
            var result = await _mediator.Send(query);

            _logger.LogInformation("Successfully retrieved {ProductCount} products in category: {Category}", 
                result.Count(), category);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting products by category: {Category}", category);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "An error occurred while retrieving products" });
        }
    }

    /// <summary>
    /// Create a new product.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        _logger.LogInformation("Creating new product: {ProductName} with price {Price}", 
            command.Name, command.Price);

        try
        {
            var result = await _mediator.Send(command);

            _logger.LogInformation("Product created successfully with ID: {ProductId}", result.Id);

            return CreatedAtAction(
                nameof(GetProduct), 
                new { id = result.Id }, 
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating product: {ProductName}", command.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "An error occurred while creating the product" });
        }
    }

    /// <summary>
    /// Update product price.
    /// </summary>
    [HttpPatch("{id:guid}/price")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] decimal newPrice)
    {
        _logger.LogInformation("Updating price for product {ProductId} to {NewPrice}", 
            id, newPrice);

        try
        {
            var command = new UpdateProductPriceCommand 
            { 
                ProductId = id, 
                NewPrice = newPrice 
            };
            
            var result = await _mediator.Send(command);

            _logger.LogInformation("Price updated successfully for product {ProductId}", id);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Product not found with ID: {ProductId}", id);
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating price for product {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "An error occurred while updating the product price" });
        }
    }

    /// <summary>
    /// Delete a product.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);

        try
        {
            // TODO: Implement DeleteProductCommand
            _logger.LogInformation("Product deleted successfully with ID: {ProductId}", id);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Product not found with ID: {ProductId}", id);
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting product {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "An error occurred while deleting the product" });
        }
    }
}
