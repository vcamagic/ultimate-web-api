using AutoMapper;
using Contracts;
using Entities.DataTransferObjects;
using Entities.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.ActionFilters;

namespace WebApi.Controllers;

[Route("api/companies/{companyId}/employees")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly IRepositoryManager _repository;
    private readonly ILoggerManager _logger;
    private readonly IMapper _mapper;
    public EmployeesController(IRepositoryManager repository, ILoggerManager logger,
        IMapper mapper)
    {
        _repository = repository;
        _logger = logger;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployeesForCompany(Guid companyId)
    {
        var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
        if (company == null)
        {
            _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
            return NotFound();
        }
        var employeesFromDb = await _repository.Employee.GetEmployeesAsync(companyId, trackChanges: false);

        return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(employeesFromDb));
    }

    [HttpGet("{id}", Name = "GetEmployeeForCompany")]
    public async Task<IActionResult> GetEmployeeForCompany(Guid companyId, Guid id)
    {
        var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
        if (company == null)
        {
            _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
            return NotFound();
        }
        var employeeFromDb = await _repository.Employee.GetEmployeeAsync(companyId, id, trackChanges: false);
        if (employeeFromDb == null)
        {
            _logger.LogInfo($"Employee with id: {id} doesn't exist in the database.");
            return NotFound();
        }

        return Ok(_mapper.Map<EmployeeDto>(employeeFromDb));
    }

    [HttpPost]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> CreateEmployeeForCompany(Guid companyId, [FromBody]
            EmployeeForCreationDto employee)
    {

        var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
        if (company == null)
        {
            _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
            return NotFound();
        }
        var employeeEntity = _mapper.Map<Employee>(employee);

        _repository.Employee.CreateEmployeeForCompany(companyId, employeeEntity);
        await _repository.SaveAsync();

        var employeeToReturn = _mapper.Map<EmployeeDto>(employeeEntity);
        return CreatedAtRoute("GetEmployeeForCompany", new
        {
            companyId,
            id = employeeToReturn.Id
        }, employeeToReturn);
    }

    [HttpDelete("{id}")]
    [ServiceFilter(typeof(ValidateEmployeeForCompanyExistsAttribute))]
    public async Task<IActionResult> DeleteEmployeeForCompany(Guid companyId, Guid id)
    {
        var employeeForCompany = HttpContext.Items["employee"] as Employee;

        _repository.Employee.DeleteEmployee(employeeForCompany);
        await _repository.SaveAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    [ServiceFilter(typeof(ValidateEmployeeForCompanyExistsAttribute))]
    public async Task<IActionResult> UpdateEmployeeForCompany(Guid companyId, Guid id, [FromBody] EmployeeForUpdateDto employee)
    {
        var employeeEntity = HttpContext.Items["employee"] as Employee;

        _mapper.Map(employee, employeeEntity);
        await _repository.SaveAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    [ServiceFilter(typeof(ValidateEmployeeForCompanyExistsAttribute))]

    public async Task<IActionResult> PartiallyUpdateEmployeeForCompany(Guid companyId, Guid id,
            [FromBody] JsonPatchDocument<EmployeeForUpdateDto> patchDoc)
    {
        if (patchDoc == null)
        {
            _logger.LogError("patchDoc object sent from client is null.");
            return BadRequest("patchDoc object is null");
        }


        var employeeEntity = HttpContext.Items["employee"] as Employee;

        var employeeToPatch = _mapper.Map<EmployeeForUpdateDto>(employeeEntity);

        patchDoc.ApplyTo(employeeToPatch, ModelState);

        TryValidateModel(employeeToPatch);

        if (!ModelState.IsValid)
        {
            _logger.LogError("Invalid model state for the EmployeeForUpdateDto object");
            return UnprocessableEntity(ModelState);
        }

        _mapper.Map(employeeToPatch, employeeEntity);

        await _repository.SaveAsync();

        return NoContent();
    }

}