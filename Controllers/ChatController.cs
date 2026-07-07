using AssistantDocumentaire1.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace AssistantDocumentaire1.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string RagServiceUrl = "http://127.0.0.1:8001";

        public ChatController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var titres = await _context.Documents
                .OrderByDescending(d => d.DateAjout)
                .Select(d => d.Titre)
                .ToListAsync();
            return View(titres);
        }

        public class QuestionRequest
        {
            public string Question { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> Demander([FromBody] QuestionRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Question))
            {
                return BadRequest();
            }

            var client = _httpClientFactory.CreateClient();
            var contenu = new StringContent(
                JsonSerializer.Serialize(new { question = req.Question }),
                Encoding.UTF8,
                "application/json");

            try
            {
                var reponseHttp = await client.PostAsync($"{RagServiceUrl}/ask", contenu);
                if (!reponseHttp.IsSuccessStatusCode)
                {
                    return Json(new { reponse = "Le service d'IA local ne répond pas. Vérifie qu'Uvicorn est bien lancé (port 8001)." });
                }
                var json = await reponseHttp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var reponseTexte = doc.RootElement.GetProperty("reponse").GetString();
                return Json(new { reponse = reponseTexte });
            }
            catch (HttpRequestException)
            {
                return Json(new { reponse = "Impossible de contacter le service Python RAG. Vérifie qu'il tourne sur http://127.0.0.1:8001." });
            }
        }
    }
}