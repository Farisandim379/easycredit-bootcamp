using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;

namespace EasyCreditApi.Interfaces.Postgres;

public class EfUserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public EfUserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(string email) => _dbContext.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(user => user.Email == email);

    public async Task AddAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }
}