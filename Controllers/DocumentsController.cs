using AssistantDocumentaire1.Data;
using AssistantDocumentaire1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantDocumentaire1.Controllers
{
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DocumentsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var documents = await _context.Documents
                .OrderByDescending(d => d.DateAjout)
                .ToListAsync();
            return View(documents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> fichiers)
        {
            if (fichiers == null || fichiers.Count == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            string dossierStockage = Path.Combine(_environment.ContentRootPath, "UploadedFiles");
            if (!Directory.Exists(dossierStockage))
            {
                Directory.CreateDirectory(dossierStockage);
            }

            foreach (var fichier in fichiers)
            {
                if (fichier.Length == 0 || Path.GetExtension(fichier.FileName).ToLower() != ".pdf")
                {
                    continue;
                }

                string nomUnique = $"{Guid.NewGuid()}_{fichier.FileName}";
                string cheminComplet = Path.Combine(dossierStockage, nomUnique);

                using (var stream = new FileStream(cheminComplet, FileMode.Create))
                {
                    await fichier.CopyToAsync(stream);
                }

                _context.Documents.Add(new Document
                {
                    Titre = fichier.FileName,
                    DateAjout = DateTime.Now,
                    Chemin = cheminComplet,
                    TailleOctets = fichier.Length,
                    EstIndexe = false
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Supprimer(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document != null)
            {
                if (System.IO.File.Exists(document.Chemin))
                {
                    System.IO.File.Delete(document.Chemin);
                }
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public class RenommerRequest
        {
            public int Id { get; set; }
            public string NouveauTitre { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> Renommer([FromBody] RenommerRequest req)
        {
            var document = await _context.Documents.FindAsync(req.Id);
            if (document != null)
            {
                document.Titre = req.NouveauTitre;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}