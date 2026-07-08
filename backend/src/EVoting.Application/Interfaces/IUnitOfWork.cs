namespace EVoting.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}
