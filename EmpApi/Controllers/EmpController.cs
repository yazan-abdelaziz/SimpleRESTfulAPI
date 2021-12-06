using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nest;
using RabbitMQ.Client;
using Shared;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EmpApi.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("MyPolicy")]
    [ApiController]
    public class EmpController : ControllerBase
    {
        private readonly EmpContext _context;
        private readonly List<Employee> _cache;
        private readonly ILogger _logger;
        private readonly ElasticClient _elasticClient;
        public EmpController(EmpContext context, ElasticClient elasticClient, ILogger<EmpController> logger)
        {
            _context = context;
            _elasticClient = elasticClient;
            _logger = logger;
            _cache = _elasticClient.Search<Employee>(s => s.MatchAll()).Documents.Select(f => f).ToList();
        }
        // GET: api/<EmpController>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = _elasticClient.Search<Employee>(s=>s.MatchAll());
            return Ok(result.Documents.Select(f => f).ToList());
        }

        // GET api/<EmpController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> Get(int id)
        {
            Employee employee = _elasticClient.Search<Employee>(s => s.DocValueFields(p => p.Fields(f => f.EmployeeId == id))).Documents.FirstOrDefault();

            if (employee == null)
            {
                return NotFound();
            }

            return Ok(employee);
        }

        // POST api/<EmpController>
        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee(Employee newEmployee)
        {
            _context.Employees.Add(newEmployee);
            await _context.SaveChangesAsync();
            PublishMessage("Create ", newEmployee.EmployeeId);
            return CreatedAtAction(
                nameof(CreateEmployee),
                new { id = newEmployee.EmployeeId });
        }

        // PUT api/<EmpController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmployee(int id, Employee employee)
        {
            if (id != employee.EmployeeId)
            {
                return BadRequest();
            }

            _context.Entry(employee).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!EmployeeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw ex;
                }
            }
            PublishMessage("Update ", employee.EmployeeId);
            return NoContent();
        }

        // DELETE api/<EmpController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Employee>> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            PublishMessage("Delete ", employee.EmployeeId);

            return employee;
        }
        private bool EmployeeExists(long id) =>
         _context.Employees.Any(e => e.EmployeeId == id);


        public void PublishMessage(string message, int id)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "task-queue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                message += id;
                var body = Encoding.UTF8.GetBytes(message);
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                channel.BasicPublish(exchange: "",
                                     routingKey: "task-queue",
                                     basicProperties: properties,
                                     body: body);
                Console.WriteLine(" [x] Sent {0}", message);
            }

            //Console.WriteLine(" Press [enter] to exit.");
            //Console.ReadLine();

        }
    }
}

/*
 * public async Task SaveManyAsync(Employee[] employees)
        {
            _cache.AddRange(employees);
            var result = await _elasticClient.IndexManyAsync(employees);
            if (result.Errors)
            {
                // the response can be inspected for errors
                foreach (var itemWithError in result.ItemsWithErrors)
                {
                    _logger.LogError("Failed to index document {0}: {1}",
                        itemWithError.Id, itemWithError.Error);
                }
            }
        }

        public async Task SaveBulkAsync(Employee[] employees)
        {
            _cache.AddRange(employees);
            var result = await _elasticClient.BulkAsync(b => b.Index("employees").IndexMany(employees));
            if (result.Errors)
            {
                // the response can be inspected for errors
                foreach (var itemWithError in result.ItemsWithErrors)
                {
                    _logger.LogError("Failed to index document {0}: {1}",
                        itemWithError.Id, itemWithError.Error);
                }
            }
        }

        public async Task SaveSingleAsync(Employee employee)
        {
            if (_cache.Any(p => p.EmployeeId == employee.EmployeeId))
            {
                await _elasticClient.UpdateAsync<Employee>(employee, u => u.Doc(employee));
                Console.WriteLine("Update part");
            }
            else
            {
                _cache.Add(employee);
                await _elasticClient.IndexDocumentAsync(employee);
            }
        }

        public async Task DeleteAsync(Employee employee)
        {
            await _elasticClient.DeleteAsync<Employee>(employee);

            if (_cache.Contains(employee))
            {
                _cache.Remove(employee);
            }
        }
*/
