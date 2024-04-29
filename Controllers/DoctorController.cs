using backend.Data;
using backend.DTO;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorController : ControllerBase
    {
        private readonly Context _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JWTService _jwtService;

        public DoctorController(Context context, IHttpContextAccessor httpContextAccessor,
        JWTService jwtService
        )
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _jwtService = jwtService;
        }

        [HttpPost("Register")]
        public async Task<ActionResult<Doctor>> RegisterDoctor(DoctorFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Doctors.AnyAsync(d => d.Email == model.Email))
            {
                return Conflict("Email already registered");
            }

            var doctor = new Doctor
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password,
                PrivateNumber = model.PrivateNumber,
                Category = model.Category,
                ImageUrl = await SaveFile(model.Image),
                CVUrl = await SaveFile(model.CV),

            };

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("Login")]
        public async Task<ActionResult<string>> Login(LoginDto model)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == model.UserName && d.Password == model.Password);
            if (doctor == null)
            {
                return Unauthorized("Invalid email or password");
            }

            // Assuming Doctor model inherits from User model or you have a separate User model
            var user = new User
            {
                Id = doctor.Id.ToString(),
                FirstName = doctor.FirstName,
                PrivateNumber = doctor.PrivateNumber,
                LastName = doctor.LastName,
                Email = doctor.Email
                // Add any other properties if needed
            };

            var jwt = _jwtService.CreateJWT(user);
            var LoggedInDoctor = new DoctorDto
            {
                Id = doctor.Id,
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                PrivateNumber = doctor.PrivateNumber,
                Email = doctor.Email,
                JWT = jwt,
                ImageUrl = GetImageUrl(doctor.ImageUrl),
                CVUrl = GetImageUrl(doctor.CVUrl),
                Type = "Doctor"

            };
            return Ok(LoggedInDoctor);
        }



        private async Task<string> SaveFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var filePath = Path.Combine(directoryPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return fileName;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctors()
        {
            var doctors = await _context.Doctors.ToListAsync();

            var doctorDtos = doctors.Select(d => new DoctorDto
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                PrivateNumber = d.PrivateNumber,
                Category = d.Category,
                ImageUrl = GetImageUrl(d.ImageUrl),
                CVUrl = GetImageUrl(d.CVUrl)
            }).ToList();

            return Ok(doctorDtos);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorDto>> GetDoctorById(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);

            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            var doctorDto = new DoctorDto
            {
                Id = doctor.Id,
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                Email = doctor.Email,
                PrivateNumber = doctor.PrivateNumber,
                Category = doctor.Category,
                ImageUrl = GetImageUrl(doctor.ImageUrl),
                CVUrl = GetImageUrl(doctor.CVUrl)
            };

            return Ok(doctorDto);
        }

        [HttpGet("images/{fileName}")]
        public IActionResult GetImage(string fileName)
        {
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileName);

            if (!System.IO.File.Exists(imagePath))
            {
                return NotFound();
            }

            var imageData = System.IO.File.OpenRead(imagePath);
            return File(imageData, "image/jpeg");
        }

        private string GetImageUrl(string relativePath)
        {
            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host.Value}";
            return $"{baseUrl}/api/Doctor/images/{relativePath}";
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDoctor(int id)
        {
            try
            {
                var doctor = await _context.Doctors.FindAsync(id);
                if (doctor == null)
                {
                    return NotFound("Doctor not found");
                }

                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Doctor deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("DeleteAll")]
        public async Task<ActionResult> DeleteAllDoctors()
        {
            try
            {
                var doctors = await _context.Doctors.ToListAsync();
                _context.Doctors.RemoveRange(doctors);
                await _context.SaveChangesAsync();

                return Ok("All doctors deleted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}