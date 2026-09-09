using Microsoft.EntityFrameworkCore;
using RetailFlow.Data;
using RetailFlow.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RetailFlow.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private User? _currentUser;

    public User? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;
    public bool IsManager => _currentUser?.Role == UserRole.StoreManager;
    public bool IsCashier => _currentUser?.Role == UserRole.Cashier;

    public event Action<User?>? CurrentUserChanged;

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<User?> AuthenticateByPinAsync(string pinCode)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PinCode == pinCode && u.IsActive);

        if (user != null)
        {
            _currentUser = user;
            CurrentUserChanged?.Invoke(_currentUser);
        }

        return user;
    }

    public async Task<User?> AuthenticateUserAsync(string username, string pinCode)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.PinCode == pinCode && u.IsActive);

        if (user != null)
        {
            _currentUser = user;
            CurrentUserChanged?.Invoke(_currentUser);
        }

        return user;
    }

    public void SwitchUser(User user)
    {
        _currentUser = user;
        CurrentUserChanged?.Invoke(_currentUser);
    }

    public void Logout()
    {
        _currentUser = null;
        CurrentUserChanged?.Invoke(null);
    }
}
