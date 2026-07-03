using GS.MultiTenant.Models;
using GS.TenantService.Contracts;
using GS.TenantService.Services;
using Microsoft.AspNetCore.Mvc;

namespace GS.TenantService.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly ITenantManagementService _tenantService;

    public TenantsController(ITenantManagementService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// Endpoint consumed by GS.MultiTenant HttpTenantConfigurationClient.
    /// </summary>
    [HttpGet("{tenantCode}")]
    [ProducesResponseType(typeof(TenantModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantModel>> GetByCode(string tenantCode, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByTenantCodeAsync(tenantCode, cancellationToken);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TenantModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TenantModel>>> GetAll(CancellationToken cancellationToken)
    {
        var tenants = await _tenantService.GetAllAsync(cancellationToken);
        return Ok(tenants);
    }

    [HttpGet("id/{tenantId:guid}")]
    [ProducesResponseType(typeof(TenantModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantModel>> GetById(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByTenantIdAsync(tenantId, cancellationToken);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TenantModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TenantModel>> Create([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByCode), new { tenantCode = tenant.TenantCode }, tenant);
    }

    [HttpPut("{tenantCode}")]
    [ProducesResponseType(typeof(TenantModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantModel>> Update(
        string tenantCode,
        [FromBody] UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.UpdateAsync(tenantCode, request, cancellationToken);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpDelete("{tenantCode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string tenantCode, CancellationToken cancellationToken)
    {
        var deleted = await _tenantService.DeleteAsync(tenantCode, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
