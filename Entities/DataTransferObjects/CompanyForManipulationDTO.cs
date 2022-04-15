using System.ComponentModel.DataAnnotations;

namespace Entities.DataTransferObjects;
public abstract class CompanyForManipulation
{
    [Required(ErrorMessage = "Company Name filed is required.")]
    [MaxLength(30, ErrorMessage = "Company Name max length is 30 characters.")]
    public string Name { get; set; }
    public string Address { get; set; }
    [Required(ErrorMessage = "Company Country filed is required.")]
    [MaxLength(30, ErrorMessage = "Company Country max length is 30 characters.")]
    public string Country { get; set; }

    public IEnumerable<EmployeeForCreationDto> Employees { get; set; }
}