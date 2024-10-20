﻿using BookApp.Models.DTOs;
using BookApp.Models.Entities;

namespace BookApp.Repositories.Users
{
    public interface IUserRepository
    {
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByIdAsync(int id);
        Task UpdateUserAsync(User user);
        Task<User> GetUserByEmailAsync(string email);
        Task AddUserAsync(User user);
        Task<UserDTO> GetUserWithLoansByIdAsync(int id);
    }
}
