using IdentityTrain2.Data;
using IdentityTrain2.Models;
using IdentityTrain2.Models.DTO;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using System.Security.Claims;
using System.Text;

namespace IdentityTrain2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ToDoListController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ToDoListController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SmtpSettings _smtpSettings;

        public ToDoListController(AppDbContext context, ILogger<ToDoListController> logger, IOptions<SmtpSettings> smtpSettings, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _smtpSettings = smtpSettings.Value;
            _userManager = userManager;
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

        [HttpPost("send-email")]
        public async Task<IActionResult> SendToDoListByEmail()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning($"Пользователь {userId} не найден.");
                return NotFound("Пользователь не найден.");
            }

            var userEmail = user.Email;

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning($"Email для пользователя {userId} не найден.");
                return BadRequest("Email не найден.");
            }

            var toDoLists = await _context.ToDoLists
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var emailBody = new StringBuilder();
            emailBody.AppendLine("Ваш список дел:");
            foreach (var toDo in toDoLists)
            {
                emailBody.AppendLine($"- {toDo.Title}:\n{toDo.Description}\n");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
            message.To.Add(new MailboxAddress(user.UserName ?? userEmail, userEmail));
            message.Subject = "Ваш список дел";
            message.Body = new TextPart("plain")
            {
                Text = emailBody.ToString()
            };

            try
            {
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Список дел отправлен по email для пользователя {userId}");
                return Ok("Список дел отправлен по email.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при отправке email: {ex.Message}");
                return StatusCode(500, "Ошибка при отправке email.");
            }
        }
    }
}
