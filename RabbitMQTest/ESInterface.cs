using Microsoft.Extensions.Configuration;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shared;

namespace RabbitMQTest
{
    public class ESInterface
    {
        private readonly ElasticClient _elasticClient;
        private readonly List<Employee> _cache;

        public ESInterface( ElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
            _cache = _elasticClient.Search<Employee>(s => s.MatchAll()).Documents.Select(f => f).ToList();
        }


        public async Task SaveSingleAsync(Employee employee)
        {
            if (_cache.Any(p => p.EmployeeId == employee.EmployeeId))
            {
                await _elasticClient.UpdateAsync<Employee>(employee, u => u.Doc(employee));
                Console.WriteLine("Update part " + employee.Name);
            }
            else
            {
                _cache.Add(employee);
                await _elasticClient.IndexDocumentAsync(employee);
            }
        }

        public async Task DeleteAsync(int id)
        {
            await _elasticClient.DeleteAsync<Employee>(id);
            Employee emp = FindInCache(id);
            if(emp != null)
            {
                if (_cache.Contains(emp))
                {
                    _cache.Remove(emp);
                }
            }
            
        }

        public Employee FindInCache(int id)
        {
            foreach(var emp in _cache)
            {
                if (emp.EmployeeId == id)
                    return emp;
            }
            return null;
        }
    }

}
