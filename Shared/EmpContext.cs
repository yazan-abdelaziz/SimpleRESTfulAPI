using Microsoft.EntityFrameworkCore;

namespace Shared
{
    public class EmpContext : DbContext
    {
        public EmpContext(DbContextOptions<EmpContext> options) : base(options)
        {

        }
        public DbSet<Employee> Employees { get; set; }
    }
}
