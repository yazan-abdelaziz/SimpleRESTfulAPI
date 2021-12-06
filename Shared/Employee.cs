using System;
using System.ComponentModel.DataAnnotations;

namespace Shared
{
    public class Employee
    {
        [Required]
        public int EmployeeId { get; set; }
        public string Name { get; set; }
        public int Salary { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }
    }
}
