using IdentityTrain2.Data;
using IdentityTrain2.Models;
using IdentityTrain2.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace IdentityTrain2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "admin")]
    public class AdminToDoListController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminToDoListController> _logger;

        public AdminToDoListController(AppDbContext context, UserManager<IdentityUser> userManager, ILogger<AdminToDoListController> logger, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _roleManager = roleManager;
        }

        [HttpGet("AllToDoLists")]
        public async Task<ActionResult<IEnumerable<ToDoList>>> GetAllToDoLists()
        {
            _logger.LogInformation("Получение всех списков дел администратором");

            return await _context.ToDoLists.ToListAsync();
        }

        [HttpDelete("DeleteToDoList/{id}")]
        public async Task<IActionResult> DeleteToDoList(int id)
        {
            _logger.LogInformation($"Удаление всех списков дел администратором");

            var toDoList = await _context.ToDoLists.Where(t => t.Id == id).FirstAsync();
            if (toDoList == null)
            {
                _logger.LogWarning($"Ни одного списка дел не найдено");
                return NotFound();
            }

            _context.ToDoLists.Remove(toDoList);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Все списки дел удалены для пользователя с ID {id}");
            return NoContent();
        }

        [HttpPost("CreateRole")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return BadRequest("Роль не может быть пустой");
            }

            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (roleExists)
            {
                return Conflict("Роль уже существует");
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                return Ok("Роль успешно создана");
            }
            else
            {
                return StatusCode(500, "Создание роли прервано");
            }
        }

        [HttpPost("AssignRole")]
        public async Task<IActionResult> AssignRole(AssignRoleDTO assignRoleDto)
        {
            var user = await _userManager.FindByIdAsync(assignRoleDto.UserId);
            if (user == null)
            {
                _logger.LogWarning($"Пользователь с ID {assignRoleDto.UserId} не найден");
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(user, assignRoleDto.Role))
            {
                await _userManager.AddToRoleAsync(user, assignRoleDto.Role);
                _logger.LogInformation($"Роль {assignRoleDto.Role} успешно присвоена пользователю с ID {assignRoleDto.UserId}");
                return Ok();
            }

            _logger.LogWarning($"Пользователь с ID {assignRoleDto.UserId} уже имеет роль {assignRoleDto.Role}");
            return BadRequest("Пользователь уже имеет указанную роль");
        }
    }
}