using System.ComponentModel.DataAnnotations;
using AccessControl.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace AccessControl.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public required string FullName { get; set; }
    public UserRole Role { get; set; }
    public bool MustChangePassword  { get; set; }
}
