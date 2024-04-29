using backend.DTO;
using backend.Models;
using backend.Services;
using Mailjet.Client.Resources;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AdminAuth.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(RegisterAdmin request)
        {
            var success = await _adminService.RegisterAdminAsync(request);
            if (!success)
            {
                return Conflict(new { message = "Admin already exists." });
            }

            return Ok(new { message = "Admin registered successfully." });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(RegisterAdmin request)
        {
            var adminDto = await _adminService.LoginAdminAsync(request);
            if (adminDto == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            return Ok(adminDto);
        }


        [HttpGet]
        [Route("getAllAdmins")]
        public async Task<IActionResult> GetAllAdmins()
        {
            var adminDtos = await _adminService.GetAllAdminsAsync();
            return Ok(adminDtos);
        }
        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var success = await _adminService.DeleteAdminByIdAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"Admin with ID {id} not found." });
            }

            return Ok(new { message = $"Admin with ID {id} deleted successfully." });
        }
        [HttpDelete]
        [Route("deleteAll")]
        public async Task<IActionResult> DeleteAllAdmins()
        {
            var success = await _adminService.DeleteAllAdminsAsync();
            if (!success)
            {
                return NotFound(new { message = "No admins found to delete." });
            }

            return Ok(new { message = "All admins deleted successfully." });
        }
        [HttpPut]
        [Route("editPassword/{id}")]
        public async Task<IActionResult> EditAdminPassword(int id, [FromBody] string newPassword)
        {
            var success = await _adminService.EditAdminPasswordAsync(id, newPassword);
            if (!success)
            {
                return NotFound(new { message = $"Admin with ID {id} not found." });
            }

            return Ok(new { message = $"Admin password with ID {id} edited successfully." });
        }

        [HttpPut]
        [Route("editEmail/{id}")]
        public async Task<IActionResult> EditAdminEmail(int id, [FromBody] string newEmail)
        {
            var success = await _adminService.EditAdminEmailAsync(id, newEmail);
            if (!success)
            {
                return NotFound(new { message = $"Admin with ID {id} not found." });
            }

            return Ok(new { message = $"Admin email with ID {id} edited successfully." });
        }
    }

}


