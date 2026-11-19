using Library.Domain;

namespace Library.Application;

// A PORT: what the use cases need from storage. The Application layer owns this interface.
public interface ILoanStore
{
    Task<List<Loan>> AllAsync();
    Task AddAsync(Loan loan);
    Task SaveAsync();
}
