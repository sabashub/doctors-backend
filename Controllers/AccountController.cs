using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Claims;
using System.Text;
using backend.Services;
using backend.Models;
using backend.Data;
using backend.DTO;
using Microsoft.AspNetCore.Http;
using TestClient;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // Require authorization for accessing these endpoints
    public class AccountController : ControllerBase
    {
        private readonly JWTService _jwtService;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly EmailService _emailService;

        private readonly MailSender _mailSender;
        private readonly IConfiguration _config;
        private readonly Context _context;


        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(
            JWTService jwtService,
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            EmailService emailService,
            IConfiguration config,
            Context context,
            IHttpContextAccessor httpContextAccessor,
            MailSender mailSender

            )
        {
            _jwtService = jwtService;
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
            _context = context;
            _mailSender = mailSender;
            _httpContextAccessor = httpContextAccessor;
        }


        [Authorize]
        [HttpGet("refresh-user-token")]
        public async Task<ActionResult<object>> RefreshUserToken()
        {
            var userEmailClaim = User.FindFirst(ClaimTypes.Email).Value;
            var user = await _userManager.FindByNameAsync(userEmailClaim);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmailClaim);
            var admin = await _context.Admins.FirstOrDefaultAsync(d => d.Email == userEmailClaim);
            string userType = admin != null ? "Admin" : doctor != null ? "Doctor" : "User";

            if (doctor != null)
            {
                return new DoctorDto
                {
                    Id = doctor.Id,
                    FirstName = doctor.FirstName,
                    LastName = doctor.LastName,
                    Email = doctor.Email,
                    PrivateNumber = doctor.PrivateNumber,
                    JWT = _jwtService.CreateJWTForDoctor(doctor),
                    ImageUrl = GetImageUrl(doctor.ImageUrl),
                    CVUrl = GetImageUrl(doctor.CVUrl),
                    Type = userType
                };
            }
            else if (admin != null)
            {
                return new AdminDto
                {
                    Id = admin.Id,
                    Email = admin.Email,
                    JWT = _jwtService.CreateJWTForAdmin(admin),
                    Type = userType
                };
            }
            else
            {
                return new UserDto
                {
                    Id = user.Id.ToString(),
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PrivateNumber = user.PrivateNumber,
                    JWT = _jwtService.CreateJWT(user),
                    Type = "User"
                };
            }
        }

        private UserDto CreateApplicationUserDto(User user, string type)
        {
            return new UserDto
            {
                Id = user.Id.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PrivateNumber = user.PrivateNumber,
                JWT = _jwtService.CreateJWT(user),
                Type = type,

            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<object>> Login(LoginDto model)
        {
            // Try to log in as a user
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user != null)
            {
                if (user.EmailConfirmed == false)
                {
                    return Unauthorized("Please confirm your email.");
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (result.Succeeded)
                {
                    return CreateApplicationUserDto(user, "User");
                }
            }

            // Try to log in as a doctor
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == model.UserName && d.Password == model.Password);
            if (doctor != null)
            {
                // Assuming Doctor model inherits from User model or you have a separate User model
                var userFromDoctor = new User
                {
                    //Id = doctor.Id.ToString(),
                    FirstName = doctor.FirstName,
                    PrivateNumber = doctor.PrivateNumber,
                    LastName = doctor.LastName,
                    Email = doctor.Email
                    // Add any other properties if needed
                };

                // Create JWT token for the doctor
                var jwt = _jwtService.CreateJWT(userFromDoctor);

                // Return doctor DTO with JWT token
                var loggedInDoctor = new DoctorDto
                {
                    Id = doctor.Id,
                    FirstName = doctor.FirstName,
                    LastName = doctor.LastName,
                    PrivateNumber = doctor.PrivateNumber,
                    Email = doctor.Email,
                    Category = doctor.Category,
                    JWT = jwt,
                    ImageUrl = GetImageUrl(doctor.ImageUrl),
                    CVUrl = GetImageUrl(doctor.CVUrl),
                    Type = "Doctor"
                };
                return Ok(loggedInDoctor);
            }

            // Try to log in as an admin
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == model.UserName && a.Password == model.Password);

            var admin_jwt = _jwtService.CreateJWTForAdmin(admin);

            var LoggedInAdmin = new AdminDto
            {
                Id = admin.Id,
                Email = admin.Email,
                JWT = admin_jwt,
                Type = "Admin"

            };
            return Ok(LoggedInAdmin);

            // If neither user, doctor, nor admin is found, return unauthorized
            return Unauthorized("Invalid username or password");
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


        [HttpDelete("users")]
        // Ensure only users with the "Admin" role can access this endpoint
        public async Task<IActionResult> DeleteAllUsers()
        {
            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    // Log or handle errors if needed
                    return BadRequest("Failed to delete users");
                }
            }
            return Ok("All users deleted successfully");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (await CheckEmailExistsAsync(model.Email))
            {
                return BadRequest($"An existing account is using {model.Email}, email address. Please try with another email address");
            }

            var emailRecord = await _context.VerifyMails.FirstOrDefaultAsync(d => d.Email == model.Email && d.Token == model.activationCode);

            if (emailRecord == null)
            {
                return BadRequest("Invalid email or activation code");
            }


            var userToAdd = new User
            {
                //  Id = model.Id.ToString(),
                FirstName = model.FirstName.ToLower(),
                LastName = model.LastName.ToLower(),
                UserName = model.Email.ToLower(),
                PrivateNumber = model.PrivateNumber.ToString(),
                Email = model.Email.ToLower(),
                EmailConfirmed = true,
            };

            // creates a user inside our AspNetUsers table inside our database
            var result = await _userManager.CreateAsync(userToAdd, model.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new JsonResult(new { title = "Registration successful", message = "Please check your email to verify your account" }));
        }
        [HttpPost("check-reset-code")]
        public async Task<IActionResult> CheckResetCode(CheckResetCodeDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized("This email address has not been registered yet");
            }

            // Check if the reset code matches
            if (user.ResetCode == model.Code)
            {
                // Codes match, return success response
                return Ok(new { codeMatched = true });
            }

            // Codes do not match, return error response
            return Ok(new { codeMatched = false });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (string.IsNullOrEmpty(model.Email)) return BadRequest("Invalid email");

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized("This email address has not been registered yet");
            if (user.EmailConfirmed == false) return BadRequest("Please confirm your email address first.");

            try
            {
                // Generate a random four-digit code
                var random = new Random();
                var code = random.Next(1000, 9999).ToString();

                // Save the code to the user's record
                user.ResetCode = code;
                await _userManager.UpdateAsync(user);

                // Compose the email body
                var body = $"<p>Hello {user.FirstName} {user.LastName},</p>" +
                           $"<p>Your reset code is: <strong>{code}</strong></p>" +
                           "<p>Please enter this code to reset your password.</p>" +
                           "<p>Thank you,</p>" +
                           $"<br>{_config["Email:ApplicationName"]}";

                var emailSend = new EmailSendDto(user.Email, "Password Reset Code", body);

                if (await _emailService.SendEmailAsync(emailSend))
                {
                    return Ok(new JsonResult(new { title = "Password reset code sent", message = "Please check your email" }));
                }

                return BadRequest("Failed to send email. Please contact admin");
            }
            catch (Exception)
            {
                return BadRequest("Failed to send email. Please contact admin");
            }
        }










        [HttpPost("verify-mail")]
        public async Task<IActionResult> verifyMail(ForgotPasswordDto model)
        {
            if (string.IsNullOrEmpty(model.Email)) return BadRequest("Invalid email");

            try
            {
                // Generate a random four-digit code
                var random = new Random();
                var code = random.Next(1000, 9999).ToString();

                var emailRecord = await _context.VerifyMails.FirstOrDefaultAsync(d => d.Email == model.Email);


                if (emailRecord != null)
                {
                    emailRecord.Token = code;
                    _context.VerifyMails.Update(emailRecord);
                    await _context.SaveChangesAsync();

                }
                else
                {

                    var VerifyEmaiInstance = new VerifyMail
                    {
                        Email = model.Email.ToLower(),
                        Token = code
                    };
                    _context.VerifyMails.Add(VerifyEmaiInstance);
                    await _context.SaveChangesAsync();

                }



                var body = $"<p>Hello</p>" +
                               $"<p>Your Confirmation code is: <strong>{code}</strong></p>" +
                               "<p>Please enter this code to Confirm Account.</p>" +
                               "<p>Thank you,</p>" +
                               $"<br>{_config["Email:ApplicationName"]}";

                var emailSend = new EmailSendDto(model.Email, "Account Activation", body);

                if (await _emailService.SendEmailAsync(emailSend))
                {
                    return Ok(new JsonResult(new { title = "Account Activation code sent", fsa = emailRecord, message = "Please check your email" }));
                }

                return BadRequest("Failed to send email. Please contact admin");
            }
            catch (Exception)
            {
                return BadRequest("Failed to send email. Please contact admin");
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            // Find the user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // User not found
                return NotFound("User not found");
            }

            // Generate a password reset token for the user
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Reset the password using the generated token and the new password
            var resetPasswordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (resetPasswordResult.Succeeded)
            {
                // Password reset successfully
                return Ok(new { message = "Password changed successfully" });
            }
            else
            {
                // Failed to reset password
                return BadRequest("Failed to reset password");
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            var userDtos = users.Select(CreateApplicationUserDto).ToList();
            return Ok(userDtos);
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Ok("User deleted successfully");
            }
            else
            {
                return BadRequest("Failed to delete user");
            }
        }

        #region Private Helper Methods
        private UserDto CreateApplicationUserDto(User user)
        {
            return new UserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PrivateNumber = user.PrivateNumber,
                JWT = _jwtService.CreateJWT(user),
                Id = user.Id
            };
        }

        private async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _userManager.Users.AnyAsync(x => x.Email == email.ToLower());
        }



        private async Task<bool> SendForgotUsernameOrPasswordEmail(User user)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = $"{_config["JWT:ClientUrl"]}/{_config["Email:ResetPasswordPath"]}?token={token}&email={user.Email}";

            var body = $"<p>Hello: {user.FirstName} {user.LastName}</p>" +
               $"<p>Username: {user.UserName}.</p>" +
               "<p>In order to reset your password, please click on the following link.</p>" +
               $"<p><a href=\"{url}\">Click here</a></p>" +
               "<p>Thank you,</p>" +
               $"<br>{_config["Email:ApplicationName"]}";

            var emailSend = new EmailSendDto(user.Email, "Forgot username or password", body);

            return await _emailService.SendEmailAsync(emailSend);
        }
        #endregion
    }
}