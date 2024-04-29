using backend.Data;
using backend.DTO;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Services
{
    public interface IAdminService
    {
        Task<bool> RegisterAdminAsync(RegisterAdmin request);
        Task<AdminDto> LoginAdminAsync(RegisterAdmin request);

        Task<List<AdminDto>> GetAllAdminsAsync();
        Task<bool> DeleteAdminByIdAsync(int id);
        Task<bool> DeleteAllAdminsAsync();
        Task<bool> EditAdminPasswordAsync(int id, string newPassword);
        Task<bool> EditAdminEmailAsync(int id, string newEmail);
    }

    public class AdminService : IAdminService
    {
        private readonly Context _context;

        public AdminService(Context context)
        {
            _context = context;
        }

        public async Task<bool> RegisterAdminAsync(RegisterAdmin request)
        {
            // Check if admin already exists
            if (_context.Admins.Any(a => a.Email == request.Email))
            {
                return false; // Admin already exists
            }

            var newAdmin = new Admin
            {
                Email = request.Email,
                Password = request.Password, // Note: In a real-world scenario, you would hash the password
                Type = "Admin" // Set the Type property
            };

            _context.Admins.Add(newAdmin);
            await _context.SaveChangesAsync();
            return true; // Admin registered successfully
        }

        public async Task<AdminDto> LoginAdminAsync(RegisterAdmin request)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == request.Email && a.Password == request.Password);
            if (admin == null)
            {
                return null; // Invalid email or password
            }

            var adminDto = new AdminDto
            {
                Id = admin.Id,
                Email = admin.Email,
                Type = admin.Type // Assuming Type is set somewhere else, it's not set during login
            };

            return adminDto;
        }

        public async Task<List<AdminDto>> GetAllAdminsAsync()
        {
            var admins = await _context.Admins.ToListAsync();
            return admins.Select(admin => new AdminDto
            {
                Id = admin.Id,
                Email = admin.Email,
                Type = admin.Type
            }).ToList();
        }
        public async Task<bool> DeleteAdminByIdAsync(int id)
        {
            var adminToDelete = await _context.Admins.FindAsync(id);
            if (adminToDelete == null)
                return false;

            _context.Admins.Remove(adminToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAllAdminsAsync()
        {
            var allAdmins = await _context.Admins.ToListAsync();
            if (allAdmins.Count == 0)
                return false;

            _context.Admins.RemoveRange(allAdmins);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> EditAdminPasswordAsync(int id, string newPassword)
        {
            var adminToEdit = await _context.Admins.FindAsync(id);
            if (adminToEdit == null)
                return false; // Admin with the provided ID not found

            adminToEdit.Password = newPassword; // Note: In a real-world scenario, you would hash the password

            await _context.SaveChangesAsync();
            return true; // Admin password edited successfully
        }

        public async Task<bool> EditAdminEmailAsync(int id, string newEmail)
        {
            var adminToEdit = await _context.Admins.FindAsync(id);
            if (adminToEdit == null)
                return false; // Admin with the provided ID not found

            adminToEdit.Email = newEmail;

            await _context.SaveChangesAsync();
            return true; // Admin email edited successfully
        }
    }
}

