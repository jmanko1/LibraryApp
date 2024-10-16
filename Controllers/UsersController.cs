using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookApp.Context;
using BookApp.Models.Entities;
using BookApp.Repositories.Users;
using System.Text;
using System.Security.Cryptography;
using BookApp.Models.ViewModels;
using BookApp.Models.DTOs;

namespace BookApp.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // GET: /Users/Register
        [HttpGet("Register")]
        public IActionResult Register()
        {
            return View();
        }

        // GET: /Users/Login
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        // GET: /Users/Profile
        [HttpGet("Profile")]
        public IActionResult Profile()
        {
            return View();
        }

        // POST: /Users/Register
        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userRepository.GetUserByUsernameAsync(model.Login);
                if (existingUser != null)
                {
                    return BadRequest(new
                    {
                        message = "Istnieje już użytkownik o takiej nazwie."
                    });
                }

                var existingEmail = await _userRepository.GetUserByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    return BadRequest(new
                    {
                        message = "Ten adres email jest już zajęty."
                    });
                }

                var user = new User
                {
                    Login = model.Login,
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Street = model.Street,
                    City = model.City
                };

                await _userRepository.AddUserAsync(user);

                return Ok();
            }

            return BadRequest();
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.GetUserByUsernameAsync(model.Login);
                if (user != null && user.PasswordHash == HashPassword(model.Password))
                {
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    HttpContext.Session.SetInt32("Role", (int) user.Role);

                    return Ok();
                }
                
                return BadRequest(new
                {
                    message = "Nieprawidłowa nazwa użytkownika lub hasło."
                });
            }

            return BadRequest();
        }

        [HttpGet("Data")]
        public async Task<ActionResult<UserDTO>> Data()
        {
            if(HttpContext.Session.GetInt32("UserId") == null)
            {
                return BadRequest(new
                {
                    message = "Nie jesteś zalogowany."
                });
            }

            int userId = (int) HttpContext.Session.GetInt32("UserId");
            var userDTO = await _userRepository.GetUserWithLoansByIdAsync(userId);

            return Ok(userDTO);
        }

        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private string Capitalize(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return char.ToUpper(str[0]) + str.Substring(1).ToLower();
        }
    }
}
