using IdentityTrain2.Data;
using IdentityTrain2.Models;
using IdentityTrain2.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IdentityTrain2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ToDoListController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ToDoListController> _logger;

        public ToDoListController(AppDbContext context, ILogger<ToDoListController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ToDoList>>> GetToDoLists()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("Был выслан список дел для {UserId}", userId);
            return await _context.ToDoLists.Where(t => t.UserId == userId).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ToDoList>> GetToDoList(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var toDoList = await _context.ToDoLists.FindAsync(id);

            if (toDoList == null || toDoList.UserId != userId)
            {
                _logger.LogWarning($"Список дел не найден или доступ был отклонён для {userId}");
                return NotFound();
            }

            _logger.LogInformation($"Список дел {id} успешно показан для {userId}");
            return toDoList;
        }

        [HttpPost]
        public async Task<ActionResult<ToDoList>> CreateToDoList(ToDoListCreateDto toDoListDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var toDoList = new ToDoList
            {
                Title = toDoListDto.Title,
                Description = toDoListDto.Description,
                UserId = userId
            };

            _context.ToDoLists.Add(toDoList);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Список дел {toDoList.Id} создан для пользователя {userId}");
            return CreatedAtAction(nameof(GetToDoList), new { id = toDoList.Id }, toDoList);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateToDoList(int id, ToDoList toDoList)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id != toDoList.Id || toDoList.UserId != userId)
            {
                _logger.LogWarning($"Обновление дела прервано! Неверный Id, либо доступ запрещён {userId}");
                return BadRequest();
            }

            _context.Entry(toDoList).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Список дел {id} обновлён для пользователя {userId}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ToDoLists.Any(e => e.Id == id))
                {
                    _logger.LogWarning($"Список дел {id} не найден для пользователя {userId}");
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteToDoList(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var toDoList = await _context.ToDoLists.FindAsync(id);

            if (toDoList == null || toDoList.UserId != userId)
            {
                _logger.LogWarning($"Удаление дела прервано! Дело не найдено или отказано в доступе для пользователя {userId}");
                return NotFound();
            }

            _context.ToDoLists.Remove(toDoList);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Дело {id} удалено для пользователя {userId}");
            return NoContent();
        }
    }
}
