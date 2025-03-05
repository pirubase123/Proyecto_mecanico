using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using mecanico_plus.Data;
using mecanico_plus.APIs;
using Microsoft.AspNetCore.Authorization; // Asegúrate de tener esta línea para usar TokenGenerator

namespace mecanico_plus.Pages.Principal.CitaVirtual
{
    [Authorize]
    public class IndexModel : PageModel
    {
  private readonly mecanico_plus.Data.local _context;
       
private readonly DbContextOptions<local> _contextOptions;

       public IndexModel(mecanico_plus.Data.local context, DbContextOptions<local> contextOptions)
       {
           _context = context;
           _contextOptions = contextOptions;
       }


        public IList<t009_cita> t009_cita { get; set; }
        public string RoomToken { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; }

        public bool IsValidToken { get; set; }
        public bool IsAnonymousUser { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!string.IsNullOrEmpty(Token))
            {
                // Validate if token exists in your system
                IsValidToken = await ValidateToken(Token);
                if (!IsValidToken)
                {
                    TempData["Error"] = "Token de cita inválido o expirado";
                    return RedirectToPage();
                }
                RoomToken = Token;

                // Check if user is anonymous (no session)
                IsAnonymousUser = string.IsNullOrEmpty(HttpContext.Session.GetString("SessionUser"));
                if (IsAnonymousUser)
                {
                    // Override layout for anonymous users
                    ViewData["Layout"] = "_AnonymousLayout";
                }
            }
            else
            {
                // Generate and store new token
                RoomToken = GenerateUniqueToken();
                await StoreToken(RoomToken);
            }

            // t009_cita = await _context.t009_cita.ToListAsync();
            return Page();
        }

        private async Task<bool> ValidateToken(string token)
        {
            var tokenEntity = await _context.t012_token
                .FirstOrDefaultAsync(t => t.f012_token == token);

            if (tokenEntity == null)
                return false;

            if (tokenEntity.IsExpired)
            {
                // Optionally remove expired token from database
                _context.t012_token.Remove(tokenEntity);
                await _context.SaveChangesAsync();
                return false;
            }

            return true;
        }

        private async Task StoreToken(string token)
        {
            var sessionUser = HttpContext.Session.GetString("SessionUser");
            var empresa = await _context.t001_usuario
                .Where(u => u.f001_correo_electronico == sessionUser)
                .Select(u => u.f001_rowid_empresa_o_persona_natural)
                .FirstOrDefaultAsync();

            var tokenEntity = new t012_token
            {
                f012_ts = DateTime.Now,
                f012_token = token,
                f012_rowid_empresa_o_persona_natural = empresa,
                f012_expiracion = DateTime.Now.AddHours(1) // Token expires in 1 hour
            };

            _context.t012_token.Add(tokenEntity);
            await _context.SaveChangesAsync();
        }

        private string GenerateUniqueToken()
        {
            return TokenGenerator.GenerateTokenId();
        }
    }
}
